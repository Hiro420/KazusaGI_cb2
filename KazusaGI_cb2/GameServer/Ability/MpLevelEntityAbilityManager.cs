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

public class MpLevelEntityAbilityManager : BaseAbilityManager
{
	private MpLevelEntity _mpLevel => (MpLevelEntity)Owner;

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => new();

	public override HashSet<string> ActiveDynamicAbilities => new();

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => new();

	public MpLevelEntityAbilityManager(Entity owner) : base(owner)
	{
		InitAbilities();
	}

	private void InitAbilities()
	{
		foreach (string abilityName in MainApp.resourceManager.GlobalCombatData!.defaultAbilities!.defaultMPLevelAbilities)
		{
			if (!string.IsNullOrWhiteSpace(abilityName))
			{
				var abilityData = MainApp.resourceManager.ConfigAbilityMap[abilityName];
				if (abilityData != null)
				{
					var config = (ConfigAbility)abilityData.Default!;
					ConfigAbilityHashMap[Utils.AbilityHash(abilityName)] = config;
					AddAbilityToEntity(Owner, config);
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