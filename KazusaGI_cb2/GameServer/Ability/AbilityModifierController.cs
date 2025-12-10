using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityModifierController
{
	public uint InstancedAbilityId { get; }
	public uint InstancedModifierId { get; }
	public ConfigAbility AbilityConfig { get; }
	public AbilityModifier ModifierConfig { get; }
	public AbilityMetaModifierChange MetaData { get; }

	public AbilityModifierController(
		uint instancedAbilityId,
		uint instancedModifierId,
		ConfigAbility abilityConfig,
		AbilityModifier modifierConfig,
		AbilityMetaModifierChange metaData)
	{
		InstancedAbilityId = instancedAbilityId;
		InstancedModifierId = instancedModifierId;
		AbilityConfig = abilityConfig;
		ModifierConfig = modifierConfig;
		MetaData = metaData;
	}
}
