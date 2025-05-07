using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using static KazusaGI_cb2.Utils.Crypto;

namespace KazusaGI_cb2.GameServer.Handlers;

// WILL ORGANIZE THIS WHOLE THING LATER

public class TeamHandler
{
    [Packet.PacketCmdId(PacketId.ChangeAvatarReq)] // for now we will have to inline the handler
    public static void HandleChangeAvatarReq(Session session, Packet packet)
    {
        ChangeAvatarReq req = packet.GetDecodedBody<ChangeAvatarReq>();
        ChangeAvatarRsp rsp = new ChangeAvatarRsp()
        {
            CurGuid = req.Guid,
            SkillId = req.SkillId
        };

        PlayerTeam targetTeam = session.player!.GetCurrentLineup();
        PlayerAvatar oldTeamLeader = targetTeam.Leader!; // old one
        PlayerAvatar newLeaderAvatar = session.player.avatarDict[req.Guid]; // new one

        AvatarEntity oldLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, oldTeamLeader)!;
        AvatarEntity newLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, newLeaderAvatar)!;

        session.SendPacket(new SceneEntityDisappearNotify()
        {
            EntityLists = { oldLeaderEntity._EntityId },
            DisappearType = VisionType.VisionReplace
        });

        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify()
        {
            AppearType = VisionType.VisionReplace
        };
        sceneEntityAppearNotify.EntityLists.Add(newLeaderEntity.ToSceneEntityInfo(session));
        session.SendPacket(sceneEntityAppearNotify);
    }


    [Packet.PacketCmdId(PacketId.SetUpAvatarTeamReq)] // for now we will have to inline the handler
    public static void HandleSetUpAvatarTeamReq(Session session, Packet packet)
    {
        SetUpAvatarTeamReq req = packet.GetDecodedBody<SetUpAvatarTeamReq>();

        AvatarTeamUpdateNotify avatarTeamUpdateNotify = new AvatarTeamUpdateNotify();
        for (uint i = 1; i <= session.player!.teamList.Count; i++)
        {
            PlayerTeam playerTeam = session.player!.teamList[(int)i - 1];
            AvatarTeam avatarTeam = new AvatarTeam()
            {
                TeamName = $"KazusaGI team {i + 1}"
            };
            if (i == req.TeamId)
            {
                foreach (ulong playerAvatarGuid in req.AvatarTeamGuidLists)
                {
                    avatarTeam.AvatarGuidLists.Add(playerAvatarGuid);
                }
            }
            else
            {
                foreach (PlayerAvatar playerAvatar in playerTeam.Avatars)
                {
                    avatarTeam.AvatarGuidLists.Add(playerAvatar.Guid);
                }
            }
            avatarTeamUpdateNotify.AvatarTeamMaps.Add(i, avatarTeam);
        }
        session.SendPacket(avatarTeamUpdateNotify);

        SetUpAvatarTeamRsp rsp = new SetUpAvatarTeamRsp()
        {
            TeamId = req.TeamId,
            CurAvatarGuid = req.CurAvatarGuid
        };

        // this is the team were working with
        PlayerTeam targetTeam = session.player!.teamList[(int)req.TeamId - 1];

        List<AvatarEntity> avatarEntities = session.entityMap.Values
            .OfType<AvatarEntity>()
            .ToList(); // get all current avatar entities


        PlayerAvatar oldTeamLeader = targetTeam.Leader!; // old one
        PlayerAvatar newLeaderAvatar = session.player.avatarDict[req.CurAvatarGuid]; // new one

        // its leader
        AvatarEntity oldLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, oldTeamLeader)!;

        AvatarEntity newLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, newLeaderAvatar)!;

        targetTeam.Avatars = new List<PlayerAvatar>(); // empty the avatars list

        SceneTeamUpdateNotify notify = new SceneTeamUpdateNotify(); // for the STUN packet

        foreach (ulong targetAvatarGuid in req.AvatarTeamGuidLists)
        {
            PlayerAvatar targetAvatar = session.player.avatarDict[targetAvatarGuid]; // the avatar we want to assign to the team
            targetTeam.AddAvatar(session, targetAvatar);
            rsp.AvatarTeamGuidLists.Add(targetAvatarGuid);

            notify.SceneTeamAvatarLists.Add(new SceneTeamAvatar()
            {
                AvatarGuid = targetAvatar.Guid,
                EntityId = avatarEntities.First(c => c.DbInfo == targetAvatar)._EntityId,
                AvatarInfo = targetAvatar.ToAvatarInfo(),
                PlayerUid = session.player.Uid,
                SceneId = session.player!.SceneId,
            });

        }

        session.player!.teamList[(int)req.TeamId - 1].Leader = newLeaderAvatar; // set the new leader

        session.SendPacket(notify);

        if (oldLeaderEntity == null)
        {
            oldLeaderEntity = session.player!.FindEntityByPlayerAvatar(session, session.player!.GetCurrentLineup().Leader!)!;
            session.SendPacket(new SceneEntityDisappearNotify()
            {
                EntityLists = { oldLeaderEntity!._EntityId },
                DisappearType = VisionType.VisionReplace
            });
        }
        else
        {
            session.SendPacket(new SceneEntityDisappearNotify()
            {
                EntityLists = { oldLeaderEntity._EntityId },
                DisappearType = VisionType.VisionReplace
            });
        }

        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify()
        {
            AppearType = VisionType.VisionReplace
        };
        sceneEntityAppearNotify.EntityLists.Add(newLeaderEntity.ToSceneEntityInfo(session));
        session.SendPacket(sceneEntityAppearNotify);

        PlayerWeapon weapon = session.player!.weaponDict[newLeaderAvatar.EquipGuid];

        // AvatarEquipChangeNotify
        session.SendPacket(new AvatarEquipChangeNotify()
        {
            AvatarGuid = newLeaderAvatar.Guid,
            EquipType = 6, // weapon
            ItemId = weapon.WeaponId,
            EquipGuid = weapon.Guid,
            Weapon = weapon.ToSceneWeaponInfo(session)
        });

		session.SendPacket(rsp);
    }

}
