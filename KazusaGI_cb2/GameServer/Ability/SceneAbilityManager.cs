using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.GameServer.Systems.Ability;
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

public class SceneAbilityManager : BaseAbilityManager
{
	private SceneEntity _scene => (SceneEntity)Owner;

	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => new();

	public override HashSet<string> ActiveDynamicAbilities => new();

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => new();

	public SceneAbilityManager(Entity owner) : base(owner)
	{
		InitAbilities();
    }

	private void InitAbilities()
	{
		// Initialize scene-specific abilities here if needed
		/*
			for (var ability :
				GameData.getConfigGlobalCombat().getDefaultAbilities().getLevelElementAbilities()) {
			AbilityData data = GameData.getAbilityData(ability);
			if (data != null)
				getScene().getWorld().getHost().getAbilityManager().addAbilityToEntity(this, data);
		}
		*/
		foreach (string abilityName in MainApp.resourceManager.GlobalCombatData!.defaultAbilities!.levelElementAbilities!)
		{
			if (!string.IsNullOrWhiteSpace(abilityName))
			{
				var abilityData = MainApp.resourceManager.ConfigAbilityMap[abilityName];
				if (abilityData != null)
				{
					ConfigAbilityHashMap[Utils.AbilityHash(abilityName)] = (ConfigAbility)abilityData.Default!;
				}
			}
		}
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// Use the base implementation for gadget ability handling
		await base.HandleAbilityInvokeAsync(invoke);
	}

	public override void Initialize()
	{
		// Initialize gadget-specific ability behavior
		base.Initialize();
	}

}