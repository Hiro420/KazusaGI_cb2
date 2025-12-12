using Newtonsoft.Json;
using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class GenerateElemBall : BaseAction
{
    [JsonProperty] public readonly int configID;
    [JsonProperty] public readonly BaseBornType born;
    [JsonProperty] public readonly object ratio;
    [JsonProperty] public readonly object baseEnergy;

    public override async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {
        await Task.Yield();

        if (srcEntity is not AvatarEntity avatar)
            return;

        avatar.GenerateElemBallByAbility(this);
    }
}
