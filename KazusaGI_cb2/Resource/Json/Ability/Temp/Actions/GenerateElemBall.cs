using Newtonsoft.Json;
using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using ProtoBuf;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class GenerateElemBall : BaseAction
{
    private Logger logger = new Logger("GenerateElemBall");
	[JsonProperty] public readonly object configID;
    [JsonProperty] public readonly BaseBornType born;
    [JsonProperty] public readonly object ratio;
    [JsonProperty] public readonly object baseEnergy;

    public override async Task Invoke(AbilityInvokeEntry invoke, string abilityName, Entity srcEntity, Entity? targetEntity = null)
    {
        try
        {
			AbilityActionGenerateElemBall generateElemBall = Serializer.Deserialize<AbilityActionGenerateElemBall>(new MemoryStream(invoke.AbilityData));

			if (srcEntity is GadgetEntity gadget && gadget.OwnerEntityId != 0)
            {
                Entity? ownerEntity = null;
                gadget.session.player.Scene.EntityManager?.TryGet(gadget.OwnerEntityId, out ownerEntity);
                if (ownerEntity != null && ownerEntity is AvatarEntity ownerAvatar)
                {
                    srcEntity = ownerAvatar;
                }
            }

            if (srcEntity is not AvatarEntity avatar)
            {
                logger.LogError($"GenerateElemBall action can only be invoked by AvatarEntity, got {srcEntity.GetType().Name}");
                return;
            }

            logger.LogSuccess($"Generating Elem Ball for Avatar ID {avatar.DbInfo.AvatarId} using Ability {abilityName}");

            avatar.GenerateElemBallByAbility(this, generateElemBall);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error in GenerateElemBall.Invoke: {ex.Message}");
        }
    }
}
