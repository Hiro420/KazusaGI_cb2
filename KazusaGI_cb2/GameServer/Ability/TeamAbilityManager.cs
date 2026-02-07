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

public class TeamAbilityManager : BaseAbilityManager
{
	private TeamEntity _scene => (TeamEntity)Owner;

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => new();

	public override HashSet<string> ActiveDynamicAbilities => new();

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => new();

	public TeamAbilityManager(Entity owner) : base(owner)
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
		foreach (string abilityName in MainApp.resourceManager.GlobalCombatData!.defaultAbilities!.defaultTeamAbilities!)
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

	public override Protocol.AbilitySyncStateInfo BuildAbilitySyncStateInfo()
	{
		var syncInfo = new Protocol.AbilitySyncStateInfo
		{
			IsInited = false  // Teams are server-controlled, abilities managed by server
		};

		// Populate AppliedModifiers: all currently instanced modifiers
		if (InstancedModifierMap.Count > 0)
		{
			foreach (var kvp in InstancedModifierMap)
			{
				var modifierController = kvp.Value;
				if (modifierController == null)
					continue;

				var appliedModifier = new Protocol.AbilityAppliedModifier
				{
					ModifierLocalId = modifierController.modifierLocalId,
					ParentAbilityEntityId = modifierController.parentAbilityEntityId,
					ParentAbilityName = new Protocol.AbilityString
					{
						Hash = GameServer.Ability.Utils.AbilityHash(modifierController.parentAbilityName)
					},
					InstancedAbilityId = modifierController.instancedAbilityId,
					InstancedModifierId = modifierController.instancedModifierId,
					ExistDuration = modifierController.existDuration,
					ApplyEntityId = modifierController.applyEntityId,
					IsAttachedParentAbility = modifierController.isAttachedParentAbility
				};

				if (!string.IsNullOrWhiteSpace(modifierController.parentAbilityOverride))
				{
					appliedModifier.ParentAbilityOverride = new Protocol.AbilityString
					{
						Hash = GameServer.Ability.Utils.AbilityHash(modifierController.parentAbilityOverride)
					};
				}

				syncInfo.AppliedModifiers.Add(appliedModifier);
			}
		}

		// DynamicValueMaps: ability special overrides and global values
		var abilitySpecialOverrideMap = AbilitySpecialOverrideMap;
		var globalValueHashMap = GlobalValueHashMap;

		if (abilitySpecialOverrideMap.Count > 0)
		{
			foreach (var kvp in abilitySpecialOverrideMap)
			{
				foreach (var specialKvp in kvp.Value)
				{
					var entry = new Protocol.AbilityScalarValueEntry
					{
						Key = new Protocol.AbilityString { Hash = specialKvp.Key },
						ValueType = AbilityScalarType.AbilityScalarTypeFloat,
						FloatValue = specialKvp.Value
					};
					syncInfo.DynamicValueMaps.Add(entry);
				}
			}
		}

		if (globalValueHashMap.Count > 0)
		{
			foreach (var kvp in globalValueHashMap)
			{
				var entry = new Protocol.AbilityScalarValueEntry
				{
					Key = new Protocol.AbilityString { Hash = kvp.Key },
					ValueType = AbilityScalarType.AbilityScalarTypeFloat,
					FloatValue = kvp.Value
				};
				syncInfo.DynamicValueMaps.Add(entry);
			}
		}

		return syncInfo;
	}

}