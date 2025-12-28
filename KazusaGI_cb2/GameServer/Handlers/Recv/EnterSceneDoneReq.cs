using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEnterSceneDoneReq
{
    [Packet.PacketCmdId(PacketId.EnterSceneDoneReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        EnterSceneDoneReq req = packet.GetDecodedBody<EnterSceneDoneReq>();
        // Validate enter-scene token just like hk4e's Player::enterSceneDone.
        if (req.EnterSceneToken != session.player!.EnterSceneToken)
        {
            session.SendPacket(new EnterSceneDoneRsp
            {
                Retcode = (int)Retcode.RetEnterSceneTokenInvalid
            });
            return;
        }

        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify
        {
            // First avatar appear on scene load uses a normal "meet"
            // vision, matching hk4e's dest_vision_type_ for standard
            // enter-scene transitions.
            AppearType = VisionType.VisionMeet
        };
        List<AvatarEntity> avatarEntities = session.player.Scene.EntityManager.Entities.Values
                .OfType<AvatarEntity>()
                .ToList();
        PlayerAvatar currentAvatar = session.player!.GetCurrentLineup().Leader!;
        AvatarEntity currentAvatarEntity = avatarEntities.First(c => c.DbInfo == currentAvatar);
        SceneEntityInfo entityInfo = currentAvatarEntity.ToSceneEntityInfo(session);
        sceneEntityAppearNotify.EntityLists.Add(entityInfo);
        sceneEntityAppearNotify.Param = entityInfo.EntityId;
        session.SendPacket(sceneEntityAppearNotify);
        session.SendPacket(new EnterSceneDoneRsp { Retcode = 0 });
    }
}