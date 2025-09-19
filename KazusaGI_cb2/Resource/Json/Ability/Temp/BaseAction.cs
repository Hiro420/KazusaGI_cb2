using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseAction : IInvocation
{
    public virtual async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {

    }
}
