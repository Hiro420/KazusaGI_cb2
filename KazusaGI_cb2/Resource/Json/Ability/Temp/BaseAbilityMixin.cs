using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public abstract class BaseAbilityMixin : IInvocation
{
    public virtual async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {
        await Task.Yield();
    }

    public virtual async Task Initialize(
        LocalIdGenerator idGenerator,
        IDictionary<uint, IInvocation> localIdToInvocationMap,
        IList<IInvocation> invokeSiteList)
    {
        await Task.Yield();
        uint id = idGenerator.GetLocalId();
        localIdToInvocationMap[id] = this;

        // Register this mixin as an invoke site so it can be
        // addressed by a sequential LocalId index like hk4e.
        invokeSiteList.Add(this);
        /*
        idGenerator.ConfigIndex = 0;
        for(BaseAction[] actions in BaseAction[][] containrer)
        {
            for(BaseAction action)
            {
                idGenerator.InitializeActionLocalIds
            }
            idGenerator.ConfigIndex++;
        }
        idGenerator.ConfigIndex = 0;
        */
    }
}
