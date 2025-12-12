using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSetUpAvatarTeamReq
{
    [Packet.PacketCmdId(PacketId.SetUpAvatarTeamReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        SetUpAvatarTeamReq req = packet.GetDecodedBody<SetUpAvatarTeamReq>();

        TeamHandler.SendAvatarTeamUpdateNotify(session, req.TeamId, req.AvatarTeamGuidLists.ToList());

        SetUpAvatarTeamRsp rsp = new SetUpAvatarTeamRsp()
        {
            TeamId = req.TeamId,
            CurAvatarGuid = req.CurAvatarGuid
        };

        // this is the team were working with
        PlayerTeam targetTeam = session.player!.teamList[(int)req.TeamId - 1];

        List<AvatarEntity> avatarEntities = session.player.Scene.EntityManager.Entities.Values
            .OfType<AvatarEntity>()
            .ToList(); // get all current avatar entities

        PlayerAvatar oldTeamLeader = targetTeam.Leader!; // old one
        PlayerAvatar newLeaderAvatar = session.player.avatarDict[req.CurAvatarGuid]; // new one

        // its leader
        AvatarEntity oldLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, oldTeamLeader)!;
        AvatarEntity? newLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, newLeaderAvatar);

        if (newLeaderEntity == null)
        {
            newLeaderEntity = new AvatarEntity(session, newLeaderAvatar);
            session.player.Scene.EntityManager.Add(newLeaderEntity);
        }

        targetTeam.Avatars = new List<PlayerAvatar>(); // empty the avatars list

        SceneTeamUpdateNotify notify = new SceneTeamUpdateNotify(); // for the STUN packet

        foreach (ulong targetAvatarGuid in req.AvatarTeamGuidLists)
        {
            PlayerAvatar targetAvatar = session.player.avatarDict[targetAvatarGuid]; // the avatar we want to assign to the team
            targetTeam.AddAvatar(session, targetAvatar);
            rsp.AvatarTeamGuidLists.Add(targetAvatarGuid);

            AvatarEntity? avatarEntity = session.player!.FindEntityByPlayerAvatar(session, targetAvatar);
            if (avatarEntity == null)
            {
                avatarEntity = new AvatarEntity(session, targetAvatar);
                session.player.Scene.EntityManager.Add(avatarEntity);
            }

            notify.SceneTeamAvatarLists.Add(new SceneTeamAvatar()
            {
                AvatarGuid = targetAvatar.Guid,
                EntityId = avatarEntity._EntityId,
                AvatarInfo = targetAvatar.ToAvatarInfo(),
                PlayerUid = session.player.Uid,
                SceneId = session.player!.SceneId,
            });
        }

        session.player!.teamList[(int)req.TeamId - 1].Leader = newLeaderAvatar; // set the new leader

        session.SendPacket(notify);

        session.player.SendSyncTeamEntityNotify(session);

        if (oldLeaderEntity == null)
        {
            oldLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, session.player!.GetCurrentLineup().Leader!)!;
            session.SendPacket(new SceneEntityDisappearNotify()
            {
                EntityLists = { oldLeaderEntity!._EntityId },
                DisappearType = Protocol.VisionType.VisionReplace
            });
        }
        else
        {
            session.SendPacket(new SceneEntityDisappearNotify()
            {
                EntityLists = { oldLeaderEntity._EntityId },
                DisappearType = Protocol.VisionType.VisionReplace
            });
        }

        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify()
        {
            AppearType = Protocol.VisionType.VisionReplace
        };
        sceneEntityAppearNotify.EntityLists.Add(newLeaderEntity.ToSceneEntityInfo(session));
        session.SendPacket(sceneEntityAppearNotify);

        TeamHandler.SendAvatarEquipChangeNotify(session, newLeaderAvatar);

        session.SendPacket(rsp);

        // Persist updated team composition and leader selection
        session.player!.SavePersistent();
    }
}