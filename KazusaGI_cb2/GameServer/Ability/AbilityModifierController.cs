using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Generic;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityModifierController
{
	public uint instancedAbilityId { get; set; }
	public uint instancedModifierId { get; set; }
	public int modifierLocalId { get; set; }
	public uint parentAbilityEntityId { get; set; }
	public string parentAbilityName { get; set; }
	public string parentAbilityOverride { get; set; }
	public float existDuration { get; set; }
	public uint applyEntityId { get; set; }
	public bool isAttachedParentAbility { get; set; }
	public ConfigAbility AbilityConfig { get; set; }
	public AbilityModifier ModifierConfig { get; set; }
	public AbilityMetaModifierChange MetaData { get; set; }

	public AbilityModifierController(
		uint instancedAbilityId,
		uint instancedModifierId,
		int modifierLocalId,
		ConfigAbility abilityConfig,
		AbilityModifier modifierConfig,
		AbilityMetaModifierChange metaData,
		uint parentAbilityEntityId = 0,
		string parentAbilityName = "",
		string parentAbilityOverride = "",
		float existDuration = 0f,
		uint applyEntityId = 0,
		bool isAttachedParentAbility = false)
	{
		this.instancedAbilityId = instancedAbilityId;
		this.instancedModifierId = instancedModifierId;
		this.modifierLocalId = modifierLocalId;
		this.parentAbilityEntityId = parentAbilityEntityId;
		this.parentAbilityName = parentAbilityName;
		this.parentAbilityOverride = parentAbilityOverride;
		this.existDuration = existDuration;
		this.applyEntityId = applyEntityId;
		this.isAttachedParentAbility = isAttachedParentAbility;
		AbilityConfig = abilityConfig;
		ModifierConfig = modifierConfig;
		MetaData = metaData;
	}
}
