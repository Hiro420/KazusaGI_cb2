using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Talent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public class GadgetAbilityManager : BaseAbilityManager
{
	private GadgetEntity _gadget => (GadgetEntity)Owner;

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => _gadget.AbilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => _gadget.ActiveDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => _gadget.UnlockedTalentParams;

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap => new(_gadget.AbilityHashMap);

	public GadgetAbilityManager(Entity owner) : base(owner)
	{
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// Use the base implementation for gadget ability handling
		await base.HandleAbilityInvokeAsync(invoke);
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

		// 2. Per-gadget abilities from ConfigGadget, mirroring
		//    hk4e's Gadget::initAbility which pulls from the
		//    gadget's config abilities list.
		var gadgetName = _gadget.gadgetExcel.jsonName;
		if (!string.IsNullOrWhiteSpace(gadgetName) &&
			resourceManager.ConfigGadgetMap != null)
		{
			var key = _gadget.serverExcelConfig!.JsonName!;
			if (resourceManager.ConfigGadgetMap.TryGetValue(key, out var configGadget) &&
				configGadget.abilities != null)
			{
				foreach (var targetAbility in configGadget.abilities)
				{
					//Console.WriteLine($"MonsterAbilityManager: Monster {monsterName} ability: ID={targetAbility.abilityID} Name={targetAbility.abilityName} Override={targetAbility.abilityOverride}");
					if (!string.IsNullOrWhiteSpace(targetAbility.abilityName))
					{
						abilityNames.Add(targetAbility.abilityName);
					}
				}
			}
		}



		// 3. Per-gadget abilities from ConfigGadget, mirroring
		//    hk4e's Gadget::initAbility which pulls from the
		//    gadget's config abilities list.
		if (!string.IsNullOrWhiteSpace(_gadget.gadgetExcel.itemJsonName) &&
			resourceManager.ConfigGadgetMap != null)
		{
			var key = _gadget.gadgetExcel!.itemJsonName!;
			if (resourceManager.ConfigGadgetMap.TryGetValue(key, out var configGadget) &&
				configGadget.abilities != null)
			{
				foreach (var targetAbility in configGadget.abilities)
				{
					//Console.WriteLine($"MonsterAbilityManager: Monster {monsterName} ability: ID={targetAbility.abilityID} Name={targetAbility.abilityName} Override={targetAbility.abilityOverride}");
					if (!string.IsNullOrWhiteSpace(targetAbility.abilityName))
					{
						abilityNames.Add(targetAbility.abilityName);
					}
				}
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
				if (!_gadget.AbilitySpecials.ContainsKey(abilityName))
				{
					_gadget.AbilitySpecials[abilityName] = new Dictionary<string, float>();
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
				AddAbilityToEntity(_gadget, ability);
			}
		}
	}

}