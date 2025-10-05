using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseAction : IInvocation
{
    // Many actions reference nested action lists; expose them here so the local id generator
    // can walk nested action graphs.
    public BaseAction[]? actions;
    public BaseAction[]? successActions;
    public BaseAction[]? failActions;

    public virtual async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {

    }
}
