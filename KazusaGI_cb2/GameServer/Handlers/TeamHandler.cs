using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
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
    public static void SendAvatarTeamUpdateNotify(Session session, uint teamId, List<ulong> avatarTeamGuidList)
    {
        AvatarTeamUpdateNotify avatarTeamUpdateNotify = new AvatarTeamUpdateNotify();
        for (uint i = 1; i <= session.player!.teamList.Count; i++)
        {
            PlayerTeam playerTeam = session.player!.teamList[(int)i - 1];
            AvatarTeam avatarTeam = new AvatarTeam()
            {
                TeamName = $"KazusaGI team {i + 1}"
            };
            if (i == teamId)
            {
                foreach (ulong playerAvatarGuid in avatarTeamGuidList)
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
    }

    public static void SendAvatarEquipChangeNotify(Session session, PlayerAvatar newLeaderAvatar)
    {
        PlayerWeapon weapon = session.player!.weaponDict[newLeaderAvatar.EquipGuid];

        // AvatarEquipChangeNotify
        session.SendPacket(new AvatarEquipChangeNotify()
        {
            AvatarGuid = newLeaderAvatar.Guid,
            EquipType = (uint)EquipType.EQUIP_WEAPON,
            ItemId = weapon.WeaponId,
            EquipGuid = weapon.Guid,
            Weapon = weapon.ToSceneWeaponInfo(session)
        });
    }
}
