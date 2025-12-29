using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseAction : IInvocation
{
    public virtual async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {

    }
}
