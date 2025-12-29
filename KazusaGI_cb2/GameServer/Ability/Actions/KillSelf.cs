using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class KillSelf : BaseAction
{
	[JsonProperty] public readonly BasePredicate[] predicates;

	public override async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
	{
		srcEntity.session.c.LogError($"[KillSelf] Invoking KillSelf action for ability {abilityName} on entity {srcEntity._EntityId}");
	}
}
