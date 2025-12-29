using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public interface IInvocation
{
    internal Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null);
}
