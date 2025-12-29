using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

/// <summary>
/// Base type for all ability mixins. In hk4e, mixins participate in the same
/// invoke_site_vec as actions; here we simply treat each mixin instance as an
/// IInvocation that ConfigAbility will append to InvokeSiteList.
/// </summary>
public abstract class BaseAbilityMixin : IInvocation
{
    public virtual async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {
        await Task.Yield();
    }
}
