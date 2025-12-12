using Newtonsoft.Json;
using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class GenerateElemBall : BaseAction
{
    private Logger logger = new Logger("GenerateElemBall");
	[JsonProperty] public readonly object configID;
    [JsonProperty] public readonly BaseBornType born;
    [JsonProperty] public readonly object ratio;
    [JsonProperty] public readonly object baseEnergy;

    public override async Task Invoke(string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {
        try
        {
            if (srcEntity is not AvatarEntity avatar)
                return;

            logger.LogSuccess($"Generating Elem Ball for Avatar ID {avatar.DbInfo.AvatarId} using Ability {abilityName}");

            avatar.GenerateElemBallByAbility(this);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error in GenerateElemBall.Invoke: {ex.Message}");
        }
    }
}
