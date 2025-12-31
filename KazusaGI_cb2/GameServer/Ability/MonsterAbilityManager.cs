using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

/// <summary>
/// Ability manager for monsters.
///
/// Mirrors the role Grasscutter's AbilityManager plays for EntityMonster,
/// but scoped to a single monster entity. Monsters currently use a single
/// ability config map derived from their JSON ability data.
/// </summary>
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
	}

	public override Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// For now, just reuse the base implementation which already mirrors
		// Grasscutter's AbilityManager.onAbilityInvoke logic.
		return base.HandleAbilityInvokeAsync(invoke);
	}

	public override void Initialize()
	{
		// Initialize monster abilities in a Grasscutter-like way by
		// collecting ability names from global combat config and
		// per-monster configuration, then resolving them to ConfigAbility.
		var resourceManager = MainApp.resourceManager;
		var abilityNames = new HashSet<string>();

		// 1. Global default non-humanoid move abilities.
		var defaultAbilities = resourceManager.GlobalCombatData?.defaultAbilities;
		if (defaultAbilities?.nonHumanoidMoveAbilities != null)
		{
			foreach (var abilityName in defaultAbilities.nonHumanoidMoveAbilities)
			{
				if (!string.IsNullOrWhiteSpace(abilityName))
					abilityNames.Add(abilityName);
			}
		}

		// 2. Elite monster ability, if applicable.
		if (_monster._monsterInfo?.isElite == true &&
			!string.IsNullOrWhiteSpace(defaultAbilities?.monterEliteAbilityName))
		{
			abilityNames.Add(defaultAbilities.monterEliteAbilityName);
		}

		// 3. Per-monster abilities from ConfigMonster, mirroring
		//    hk4e's Monster::initAbility which pulls from the
		//    monster's config abilities list.
		var monsterName = _monster.excelConfig.monsterName;
		if (!string.IsNullOrWhiteSpace(monsterName) &&
			resourceManager.ConfigMonsterMap != null)
		{
			var key = _monster.serverExcelConfig!.CombatConfig!;
			if (resourceManager.ConfigMonsterMap.TryGetValue(key, out var configMonster) &&
				configMonster.abilities != null)
			{
				foreach (var targetAbility in configMonster.abilities)
				{
					//Console.WriteLine($"MonsterAbilityManager: Monster {monsterName} ability: ID={targetAbility.abilityID} Name={targetAbility.abilityName} Override={targetAbility.abilityOverride}");
					if (!string.IsNullOrWhiteSpace(targetAbility.abilityName))
					{
						abilityNames.Add(targetAbility.abilityName);
					}
				}
			}
			foreach (uint affixId in _monster.excelConfig.affix)
			{
				if (!resourceManager.MonsterAffixExcel.TryGetValue(affixId, out MonsterAffixExcelConfig? affix))
					continue;
				if (!string.IsNullOrWhiteSpace(affix.abilityName))
					abilityNames.Add(affix.abilityName);
            }
        }


		// 4. Resolve ability names to ConfigAbility using ResourceManager.ConfigAbilityMap.
		var configAbilityMap = resourceManager.ConfigAbilityMap;
		if (configAbilityMap != null)
		{
			foreach (var abilityName in abilityNames)
			{
				if (!configAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) ||
					container == null ||
					container.Default == null)
				{
					continue;
				}

				if (container.Default is not ConfigAbility configAbility)
					continue;

				uint hash = Utils.AbilityHash(abilityName);
				ConfigAbilityHashMap[hash] = configAbility;

				// Ensure we have a specials map entry for this ability name so that
				// BaseAbilityManager.Initialize can build override maps.
				if (!_abilitySpecials.ContainsKey(abilityName))
				{
					_abilitySpecials[abilityName] = new Dictionary<string, float>();
				}
			}
		}

		// Let the base manager build AbilitySpecialOverrideMap / AbilitySpecialHashMap
		// from the populated AbilitySpecials.
		base.Initialize();

		// Finally, mirror hk4e's Monster::initAbility by attaching all
		// resolved config abilities to the monster entity's AbilityComp.
		foreach (var kvp in ConfigAbilityHashMap)
		{
			var ability = kvp.Value;
			if (ability != null)
			{
				AddAbilityToEntity(_monster, ability);
			}
		}
	}
}