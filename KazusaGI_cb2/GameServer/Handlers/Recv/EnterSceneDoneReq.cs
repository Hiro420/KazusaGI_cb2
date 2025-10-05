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
        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify();
        List<AvatarEntity> avatarEntities = session.entityMap.Values
                .OfType<AvatarEntity>()
                .ToList();
        PlayerAvatar currentAvatar = session.player!.GetCurrentLineup().Leader!;
        AvatarEntity currentAvatarEntity = avatarEntities.First(c => c.DbInfo == currentAvatar);
        SceneEntityInfo entityInfo = currentAvatarEntity.ToSceneEntityInfo(session);
        sceneEntityAppearNotify.EntityLists.Add(entityInfo);
        sceneEntityAppearNotify.Param = entityInfo.EntityId;
        ScenePlayerLocationNotify scenePlayerLocationNotify = new ScenePlayerLocationNotify()
        {
            SceneId = session.player.SceneId
        };
        scenePlayerLocationNotify.PlayerLocLists.Add(new PlayerLocationInfo()
        {
            Uid = session.player.Uid,
            Pos = Session.Vector3ToVector(session.player.Pos),
            Rot = Session.Vector3ToVector(session.player.Rot),
        });
        session.SendPacket(sceneEntityAppearNotify);
        session.SendPacket(scenePlayerLocationNotify);
        session.SendPacket(new EnterSceneDoneRsp());
    }
}