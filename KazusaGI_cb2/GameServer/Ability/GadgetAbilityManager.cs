using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.Protocol;
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

	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap => _gadget.AbilityHashMap;

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
		// First, initialize abilities for this gadget from ConfigAbilityHashMap
		InitializeGadgetAbilities();
		
		// Initialize gadget-specific ability behavior
		base.Initialize();
	}

	/// <summary>
	/// Override the base InitializeEntityAbilities to use gadget-specific initialization
	/// </summary>
	public override void InitializeEntityAbilities(Entity entity)
	{
		// Call the gadget-specific ability initialization
		InitializeGadgetAbilities();
	}

	/// <summary>
	/// Initialize abilities for this gadget by adding them to the entity's InstancedAbilities collection
	/// </summary>
	private void InitializeGadgetAbilities()
	{
		try
		{
			// Clear any existing abilities to avoid duplicates
			Owner.InstancedAbilities.Clear();
			InstanceToAbilityHashMap.Clear();
			
			logger.LogInfo($"Initializing abilities for gadget {_gadget._gadgetId}", false);
			
			// Add abilities from the gadget's ability hash map
			foreach (var abilityEntry in ConfigAbilityHashMap)
			{
				uint abilityHash = abilityEntry.Key;
				ConfigAbility configAbility = abilityEntry.Value;
				
				// Create ability data
				var abilityData = new AbilityData(configAbility.abilityName, configAbility.modifiers);
				
				// Create instanced ability
				var instancedAbility = new InstancedAbility(abilityData);
				
				// Add to entity's instanced abilities
				Owner.InstancedAbilities.Add(instancedAbility);
				
				// Map instance ID (1-based) to ability hash
				uint instanceId = (uint)Owner.InstancedAbilities.Count;
				InstanceToAbilityHashMap[instanceId] = abilityHash;
				
				logger.LogInfo($"Added gadget ability '{configAbility.abilityName}' with hash {abilityHash} at instance {instanceId}", false);
			}
			
			logger.LogInfo($"Gadget {_gadget._gadgetId} initialized with {Owner.InstancedAbilities.Count} abilities", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to initialize gadget abilities: {ex.Message}");
		}
	}

}