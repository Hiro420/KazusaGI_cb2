using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleChangeAvatarReq
{
    [Packet.PacketCmdId(PacketId.ChangeAvatarReq)]
    public static void OnPacket(Session session, Packet packet)
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

        // Prepare notification for old avatar + weapon despawn
        var disappearNotify = new SceneEntityDisappearNotify()
        {
            DisappearType = Protocol.VisionType.VisionReplace
        };
        disappearNotify.EntityLists.Add(oldLeaderEntity._EntityId);

        // Also despawn the old leader's weapon if equipped
        if (oldTeamLeader.EquipGuid != 0 && session.player.weaponDict.TryGetValue(oldTeamLeader.EquipGuid, out var oldWeapon))
        {
            disappearNotify.EntityLists.Add(oldWeapon.WeaponEntityId);
        }

        session.SendPacket(disappearNotify);

        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify()
        {
            AppearType = Protocol.VisionType.VisionReplace
        };
        sceneEntityAppearNotify.EntityLists.Add(newLeaderEntity.ToSceneEntityInfo(session));

        // Also spawn the new leader's weapon if equipped
        if (newLeaderAvatar.EquipGuid != 0 && session.player.weaponDict.TryGetValue(newLeaderAvatar.EquipGuid, out PlayerWeapon? newWeapon))
        {
            sceneEntityAppearNotify.EntityLists.Add(newWeapon.WeaponEntity!.ToSceneEntityInfo());
        }

        session.SendPacket(sceneEntityAppearNotify);
    }
}