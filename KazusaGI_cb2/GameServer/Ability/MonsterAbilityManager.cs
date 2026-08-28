using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Monster;

namespace KazusaGI_cb2.GameServer.Ability;

public class MonsterAbilityManager : BaseAbilityManager
{
	private readonly MonsterEntity _monster;
	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();
	private readonly Dictionary<string, Dictionary<string, float>?> _abilitySpecials = new();
	private readonly HashSet<string> _activeDynamicAbilities = new();
	private readonly Dictionary<string, HashSet<string>> _unlockedTalentParams = new();
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => _abilitySpecials;
	public override HashSet<string> ActiveDynamicAbilities => _activeDynamicAbilities;
	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => _unlockedTalentParams;

	public MonsterAbilityManager(MonsterEntity owner) : base(owner)
	{
		_monster = owner;
		InitAbilities();
	}

	public override Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		return base.HandleAbilityInvokeAsync(invoke);
	}

	private void InitAbilities()
	{
		ResourceManager resourceManager = MainApp.resourceManager;
		Dictionary<string, ConfigAbilityContainer>? configAbilityMap = resourceManager.ConfigAbilityMap;
		if (configAbilityMap == null)
			return;

		ConfigMonster? configMonster = null;
		string? combatConfig = _monster.serverExcelConfig?.CombatConfig;
		if (!string.IsNullOrWhiteSpace(combatConfig))
			resourceManager.ConfigMonsterMap.TryGetValue(combatConfig, out configMonster);
		if (configMonster == null)
		{
			logger.LogWarning($"getJsonConfig failed for monster {_monster._monsterId}, combatConfig={combatConfig ?? "<null>"}");
			return;
		}

		// hk4e Monster::affix_set_ is std::set<uint32_t>, so both pre-add and
		// dynamic affix passes observe ascending affix ids with duplicates removed.
		SortedSet<uint> affixIds = BuildAffixSet();

		// AbilityComp::addAllPreDynamicConfigAbilities().
		foreach (uint affixId in affixIds)
		{
			if (!resourceManager.MonsterAffixExcel.TryGetValue(affixId, out MonsterAffixExcelConfig? affix))
			{
				logger.LogWarning($"Monster affix {affixId} was not found for monster {_monster._monsterId}");
				return;
			}

			if (affix.preAdd && !TryAttachConfigAbility(affix.abilityName, null, configAbilityMap))
				return;
		}

		// AbilityComp::addAllStaticConfigAbilities(). For monsters, hk4e's
		// Monster::initAbility marks the creature non-humanoid once ConfigMonster
		// is loaded and copies ConfigMonster::abilities into ability_entry_vec_.
		// Build the complete static list first, then attach it in list order.
		var staticAbilities = new List<(string AbilityName, string? AbilityOverride)>();
		List<string>? nonHumanoidAbilities = resourceManager.GlobalCombatData?.defaultAbilities?.nonHumanoidMoveAbilities;
		if (nonHumanoidAbilities != null)
		{
			foreach (string abilityName in nonHumanoidAbilities)
				staticAbilities.Add((abilityName, null));
		}

		if (configMonster.abilities != null)
		{
			var abilityIdSet = new HashSet<string>(StringComparer.Ordinal);
			foreach (TargetAbility entry in configMonster.abilities)
			{
				// hk4e validates findAbilityConfig before checking ability_id_map_.
				// A bad config entry is logged and skipped instead of aborting the
				// whole static pass.
				if (!CanResolveConfigAbility(entry.abilityName, entry.abilityOverride, configAbilityMap))
					continue;

				// hk4e's ability_id_map_ key is abilityID + "_" + entityId.
				// EntityId is constant for this manager, so every abilityID value
				// (including empty) is admitted only once, in vector order.
				string abilityId = entry.abilityID ?? string.Empty;
				if (!abilityIdSet.Add(abilityId))
					continue;

				staticAbilities.Add((entry.abilityName, entry.abilityOverride));
			}
		}

		foreach ((string abilityName, string? abilityOverride) in staticAbilities)
		{
			if (!TryAttachConfigAbility(abilityName, abilityOverride, configAbilityMap))
				return;
		}

		// AbilityComp::addAllDynamicConfigAbilities() monster branch: elite
		// ability first, then every non-pre-add affix in affix_set_ order.
		if (_monster._monsterInfo?.isElite == true)
		{
			string? eliteAbilityName = resourceManager.GlobalCombatData?.defaultAbilities?.monterEliteAbilityName;
			if (!TryAttachConfigAbility(eliteAbilityName, null, configAbilityMap))
				return;
		}

		foreach (uint affixId in affixIds)
		{
			MonsterAffixExcelConfig affix = resourceManager.MonsterAffixExcel[affixId];
			if (!affix.preAdd && !TryAttachConfigAbility(affix.abilityName, null, configAbilityMap))
				return;
		}
	}

	private SortedSet<uint> BuildAffixSet()
	{
		var affixIds = new SortedSet<uint>();

		if (_monster.excelConfig.affix != null)
		{
			foreach (uint affixId in _monster.excelConfig.affix)
			{
				if (affixId != 0)
					affixIds.Add(affixId);
			}
		}

		if (_monster._monsterInfo?.affix != null)
		{
			foreach (uint affixId in _monster._monsterInfo.affix)
			{
				if (affixId != 0)
					affixIds.Add(affixId);
			}
		}

		return affixIds;
	}

	private bool CanResolveConfigAbility(
		string? abilityName,
		string? abilityOverride,
		Dictionary<string, ConfigAbilityContainer> configAbilityMap)
	{
		if (!string.IsNullOrWhiteSpace(abilityName) &&
			configAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) &&
			container?.Default is ConfigAbility)
		{
			return true;
		}

		logger.LogWarning($"findAbilityConfig failed for monster {_monster._monsterId}: ability={abilityName ?? "<null>"}, override={abilityOverride ?? "Default"}");
		return false;
	}

	private bool TryAttachConfigAbility(
		string? abilityName,
		string? abilityOverride,
		Dictionary<string, ConfigAbilityContainer> configAbilityMap)
	{
		if (string.IsNullOrWhiteSpace(abilityName))
		{
			logger.LogWarning($"Monster {_monster._monsterId} has an empty ability name during ability initialization");
			return false;
		}

		if (!configAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) ||
			container?.Default is not ConfigAbility configAbility)
		{
			logger.LogWarning($"findAbilityConfig failed for monster {_monster._monsterId}: ability={abilityName}, override={abilityOverride ?? "Default"}");
			return false;
		}

		string? normalizedOverride = string.IsNullOrWhiteSpace(abilityOverride) ||
			string.Equals(abilityOverride, "Default", StringComparison.Ordinal)
			? null
			: abilityOverride;
		uint overrideHash = normalizedOverride == null ? 0u : Utils.AbilityHash(normalizedOverride);

		ActorAbility? actorAbility = AttachActorAbility(configAbility, normalizedOverride, 0, overrideHash);
		if (actorAbility == null)
			return false;

		if (!_abilitySpecials.ContainsKey(configAbility.abilityName))
			_abilitySpecials[configAbility.abilityName] = BuildAbilitySpecials(configAbility);

		// Compatibility runtime used by the current action dispatcher. Its order
		// now follows the same ActorAbility attach order instead of a hash sort.
		AddAbilityToEntity(_monster, configAbility);
		return true;
	}

	private static Dictionary<string, float> BuildAbilitySpecials(ConfigAbility config)
	{
		var specials = new Dictionary<string, float>();
		if (config.abilitySpecials == null)
			return specials;
		foreach (var kvp in config.abilitySpecials)
		{
			if (kvp.Value.TryGetSingle(out var value))
				specials[kvp.Key] = value;
		}
		return specials;
	}

	private static bool TryReadSpecialValue(object? valueObj, out float value)
	{
		switch (valueObj)
		{
			case null:
				value = 0f;
				return false;
			case float floatValue:
				value = floatValue;
				return true;
			case int intValue:
				value = intValue;
				return true;
			case long longValue:
				value = longValue;
				return true;
			case double doubleValue:
				value = (float)doubleValue;
				return true;
			case decimal decimalValue:
				value = (float)decimalValue;
				return true;
			case string stringValue:
				return float.TryParse(stringValue, out value);
			default:
				value = 0f;
				return false;
		}
	}
}