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

public class MonsterAbilityManager : BaseAbilityManager
{
	private MonsterEntity _monster => (MonsterEntity)Owner;
	
	// For now, use empty collections - monsters can be extended later with actual ability configs
	private readonly Dictionary<string, Dictionary<string, float>?>? _abilitySpecials = new();
	private readonly HashSet<string> _activeDynamicAbilities = new();
	private readonly Dictionary<string, HashSet<string>> _unlockedTalentParams = new();
	private readonly Dictionary<uint, ConfigAbility> _configAbilityHashMap = new();

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => _abilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => _activeDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => _unlockedTalentParams;

	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap => _configAbilityHashMap;

	public MonsterAbilityManager(MonsterEntity owner) : base(owner)
	{
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// Use the base implementation for monster ability handling
		await base.HandleAbilityInvokeAsync(invoke);
	}

	public override void Initialize()
	{
		// First, initialize abilities for this monster from any available configs
		InitializeMonsterAbilities();
		
		// Initialize monster-specific ability behavior
		base.Initialize();
	}

	/// <summary>
	/// Override the base InitializeEntityAbilities to use monster-specific initialization
	/// </summary>
	public override void InitializeEntityAbilities(Entity entity)
	{
		// Call the monster-specific ability initialization
		InitializeMonsterAbilities();
	}

	/// <summary>
	/// Initialize abilities for this monster by adding them to the entity's InstancedAbilities collection
	/// TODO: Expand this to load actual monster ability configs when available
	/// </summary>
	private void InitializeMonsterAbilities()
	{
		try
		{
			// Clear any existing abilities to avoid duplicates
			Owner.InstancedAbilities.Clear();
			InstanceToAbilityHashMap.Clear();
			
			logger.LogInfo($"Initializing abilities for monster {_monster._monsterId}", false);
			
			// For now, monsters don't have specific ability configs loaded
			// This can be expanded later when monster ability configurations are available
			// Example: Load abilities based on monster ID, type, or AI configuration
			
			logger.LogInfo($"Monster {_monster._monsterId} initialized with {Owner.InstancedAbilities.Count} abilities", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to initialize monster abilities: {ex.Message}");
		}
	}
}