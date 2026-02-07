using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
		var resourceManager = MainApp.resourceManager;
		var abilityNames = new HashSet<string>(StringComparer.Ordinal);

		var defaultAbilities = resourceManager.GlobalCombatData?.defaultAbilities;
		// Only add nonHumanoidMoveAbilities (like Unique_AbilityCrash) if monster has specific move types
		if (defaultAbilities?.nonHumanoidMoveAbilities != null)
		{
			// Check if monster has a valid non-humanoid move config
			var monsterCombatConfig = _monster.serverExcelConfig?.CombatConfig;
			bool hasNonHumanoidMove = false;
			if (!string.IsNullOrWhiteSpace(monsterCombatConfig) &&
			    resourceManager.ConfigMonsterMap.TryGetValue(monsterCombatConfig, out var monsterConfig))
			{
				hasNonHumanoidMove = monsterConfig.HasNonHumanoidMove();
			}
			
			if (hasNonHumanoidMove)
			{
				foreach (var abilityName in defaultAbilities.nonHumanoidMoveAbilities)
				{
					if (!string.IsNullOrWhiteSpace(abilityName))
						abilityNames.Add(abilityName);
				}
			}
		}

		if (_monster._monsterInfo?.isElite == true &&
			!string.IsNullOrWhiteSpace(defaultAbilities?.monterEliteAbilityName))
		{
			abilityNames.Add(defaultAbilities.monterEliteAbilityName);
		}

		var configAbilityMap = resourceManager.ConfigAbilityMap;
		var combatConfig = _monster.serverExcelConfig?.CombatConfig;
		if (configAbilityMap != null &&
			!string.IsNullOrWhiteSpace(combatConfig) &&
			resourceManager.ConfigMonsterMap.TryGetValue(combatConfig, out ConfigMonster? configMonster) &&
			configMonster.abilities != null)
		{
			foreach (var entry in configMonster.abilities)
			{
				var resolved = ResolveAbilityName(entry, configAbilityMap);
				if (!string.IsNullOrWhiteSpace(resolved))
					abilityNames.Add(resolved);
			}
		}

		var affixIds = new HashSet<uint>();
		foreach (var affixId in _monster.excelConfig.affix)
			if (affixId != 0)
				affixIds.Add(affixId);
		if (_monster._monsterInfo?.affix != null)
		{
			foreach (var affixId in _monster._monsterInfo.affix)
				if (affixId != 0)
					affixIds.Add(affixId);
		}

		foreach (var affixId in affixIds)
		{
			if (!resourceManager.MonsterAffixExcel.TryGetValue(affixId, out MonsterAffixExcelConfig? affix))
				continue;
			if (!string.IsNullOrWhiteSpace(affix.abilityName))
				abilityNames.Add(affix.abilityName);
		}

		if (configAbilityMap != null)
		{
			foreach (var abilityName in abilityNames)
			{
				if (!configAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) ||
					container?.Default is not ConfigAbility configAbility)
					continue;
				uint hash = Utils.AbilityHash(abilityName);
				ConfigAbilityHashMap[hash] = configAbility;
				if (!_abilitySpecials.ContainsKey(configAbility.abilityName))
					_abilitySpecials[configAbility.abilityName] = BuildAbilitySpecials(configAbility);
			}
		}

		foreach (var ability in ConfigAbilityHashMap.Values)
		{
			if (ability != null)
				AddAbilityToEntity(_monster, ability);
		}
	}

	private static string? ResolveAbilityName(TargetAbility entry, Dictionary<string, ConfigAbilityContainer> configMap)
	{
		if (!string.IsNullOrWhiteSpace(entry.abilityOverride) && configMap.ContainsKey(entry.abilityOverride))
			return entry.abilityOverride;
		if (!string.IsNullOrWhiteSpace(entry.abilityName))
			return entry.abilityName;
		return null;
	}

	private static Dictionary<string, float> BuildAbilitySpecials(ConfigAbility config)
	{
		var specials = new Dictionary<string, float>();
		if (config.abilitySpecials == null)
			return specials;
		foreach (var kvp in config.abilitySpecials)
		{
			if (TryReadSpecialValue(kvp.Value, out var value))
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