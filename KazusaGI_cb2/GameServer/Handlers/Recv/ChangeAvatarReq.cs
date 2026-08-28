using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleChangeAvatarReq
{
	[Packet.PacketCmdId(PacketId.ChangeAvatarReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		ChangeAvatarReq req = packet.GetDecodedBody<ChangeAvatarReq>();
		var rsp = new ChangeAvatarRsp
		{
			CurGuid = req.Guid,
			SkillId = req.SkillId
		};

		Player player = session.player!;
		PlayerTeam team = player.GetCurrentLineup();

		if (!player.avatarDict.TryGetValue(req.Guid, out var nextAvatar))
		{
			rsp.Retcode = 104; // RET_AVATAR_NOT_EXIST
			session.SendPacket(rsp);
			return;
		}
		if (!team.Avatars.Contains(nextAvatar))
		{
			rsp.Retcode = 122; // target is not in current lineup
			session.SendPacket(rsp);
			return;
		}

		PlayerAvatar? previousAvatar = team.Leader;
		if (previousAvatar == null)
		{
			rsp.Retcode = -1;
			session.SendPacket(rsp);
			return;
		}
		if (previousAvatar.Guid == nextAvatar.Guid)
		{
			session.SendPacket(rsp);
			return;
		}

		Scene scene = player.Scene;
		AvatarEntity previousEntity = scene.GetOrCreateAvatarEntity(previousAvatar);
		AvatarEntity nextEntity = scene.GetOrCreateAvatarEntity(nextAvatar);

		// PlayerAvatarComp::changeCurAvatar asks Scene to disappear only the
		// old Avatar entity. Its equipped WeaponGadget stays instantiated and
		// is represented through SceneAvatarInfo::weapon.
		var disappear = new SceneEntityDisappearNotify { DisappearType = VisionType.VisionReplace };
		disappear.EntityLists.Add(previousEntity._EntityId);
		session.SendPacket(disappear);

		// Persistent lineup selection changes before the replacement appears.
		team.Leader = nextAvatar;

		var appear = new SceneEntityAppearNotify { AppearType = VisionType.VisionReplace };
		appear.EntityLists.Add(nextEntity.ToSceneEntityInfo(session));
		session.SendPacket(appear);

		TeamHandler.SendAvatarEquipChangeNotify(session, nextAvatar);
		session.SendPacket(rsp);
		player.SavePersistent();
	}
}
