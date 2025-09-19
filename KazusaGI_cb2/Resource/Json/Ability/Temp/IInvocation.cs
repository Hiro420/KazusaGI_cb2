using KazusaGI_cb2.GameServer;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public interface IInvocation
{
    internal Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null);
}
