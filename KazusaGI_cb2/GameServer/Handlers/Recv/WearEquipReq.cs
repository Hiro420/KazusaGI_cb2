using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleWearEquipReq
{
    [Packet.PacketCmdId(PacketId.WearEquipReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        WearEquipReq req = packet.GetDecodedBody<WearEquipReq>();

        var player = session.player;
        if (player == null)
        {
            return;
        }

        // Find target avatar
        if (!player.avatarDict.TryGetValue(req.AvatarGuid, out PlayerAvatar? avatar))
        {
            // Avatar not found; send failure
            session.SendPacket(new WearEquipRsp
            {
                Retcode = (int)Retcode.RetCanNotFindAvatar,
                AvatarGuid = req.AvatarGuid,
                EquipGuid = req.EquipGuid
            });
            return;
        }

		// The equip is either a relic or a weapon
        if (!player.weaponDict.ContainsKey(req.EquipGuid)) // && !player.relicDict.ContainsKey(req.EquipGuid)
		{
            session.SendPacket(new WearEquipRsp
            {
                Retcode = (int)Retcode.RetItemNotExist,
                AvatarGuid = req.AvatarGuid,
                EquipGuid = req.EquipGuid
            });
            return;
		}

        ulong newEquipGuid = req.EquipGuid;

		if (player.weaponDict.TryGetValue(req.EquipGuid, out PlayerWeapon? weapon))
        {
			// If avatar already has a weapon, detach it first
			if (avatar.EquipGuid != 0 && player.weaponDict.TryGetValue(avatar.EquipGuid, out var oldWeapon))
			{
				oldWeapon.EquipGuid = null;
			}

            // For safety ig?
			newEquipGuid = weapon.Guid;

			// Equip the new weapon and broadcast change
			weapon.EquipOnAvatar(avatar, broadcastPacket: true);
		}

		// Recalculate fight props for the avatar based on new weapon
		avatar.ReCalculateFightProps();

		// Broadcast avatar info update
        avatar.BroadcastPropUpdate();

		// Send response
		session.SendPacket(new WearEquipRsp
        {
            Retcode = 0,
            AvatarGuid = avatar.Guid,
            EquipGuid = newEquipGuid
        });

		// If the player is in current team, update the team avatar info 
        if (player.IsInCurrentTeam(avatar.Guid))
        {
            player.SendSceneTeamUpdateNotify(session);
		}

		// Persist equipment change so it survives relog
		player.SavePersistent();
	}
}
