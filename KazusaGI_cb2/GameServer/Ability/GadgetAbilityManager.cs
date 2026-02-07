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

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap => _gadget.AbilityHashMap;

	public GadgetAbilityManager(Entity owner) : base(owner)
	{
		InitAbilities();
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// Use the base implementation for gadget ability handling
		await base.HandleAbilityInvokeAsync(invoke);
	}

	private void InitAbilities()
	{
		var resourceManager = MainApp.resourceManager;
		var abilityNames = new HashSet<string>();

		// Only add if gadget has specific move types (ConfigSimpleMove, ConfigRigidBodyMove, ConfigAnimatorMove, ConfigMixinDriveMove)
		var defaultAbilities = resourceManager.GlobalCombatData?.defaultAbilities;
		if (defaultAbilities?.nonHumanoidMoveAbilities != null)
		{
			// Check if gadget has a valid non-humanoid move config
			var gadgetJsonName = _gadget.gadgetExcel.jsonName;
			bool hasNonHumanoidMove = false;
			if (!string.IsNullOrWhiteSpace(gadgetJsonName) &&
			    resourceManager.ConfigGadgetMap != null)
			{
				var configKey = _gadget.serverExcelConfig!.JsonName!;
				if (resourceManager.ConfigGadgetMap.TryGetValue(configKey, out var gadgetConfig))
				{
					hasNonHumanoidMove = gadgetConfig.HasNonHumanoidMove();
				}
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

				// Seed ability specials from config defaults.
				if (!_gadget.AbilitySpecials.ContainsKey(abilityName))
				{
					_gadget.AbilitySpecials[abilityName] = BuildAbilitySpecials(configAbility);
				}
			}
		}

		// Finally, mirror hk4e's Gadget::initAbility by attaching all
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