using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSetUpAvatarTeamReq
{
	[Packet.PacketCmdId(PacketId.SetUpAvatarTeamReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		SetUpAvatarTeamReq req = packet.GetDecodedBody<SetUpAvatarTeamReq>();
		var rsp = new SetUpAvatarTeamRsp
		{
			TeamId = req.TeamId,
			CurAvatarGuid = req.CurAvatarGuid
		};

		Player player = session.player!;
		if (req.TeamId == 0 || req.TeamId > player.teamList.Count)
		{
			rsp.Retcode = 120; // RET_AVATAR_TEAM_NOT_EXIST
			session.SendPacket(rsp);
			return;
		}

		var requestedGuids = req.AvatarTeamGuidLists.ToList();
		// PlayerAvatarComp::checkAvatarTeamCanSetUp has a hard defensive cap
		// of 0x27 entries. Normal gameplay config imposes the smaller usable
		// party size; this low-level request path does not hard-code four.
		if (requestedGuids.Count == 0 || requestedGuids.Count > 0x27)
		{
			rsp.Retcode = -1;
			session.SendPacket(rsp);
			return;
		}
		if (requestedGuids.Distinct().Count() != requestedGuids.Count)
		{
			rsp.Retcode = 106;
			session.SendPacket(rsp);
			return;
		}
		if (!requestedGuids.Contains(req.CurAvatarGuid))
		{
			rsp.Retcode = 122;
			session.SendPacket(rsp);
			return;
		}

		var requestedAvatars = new List<PlayerAvatar>(requestedGuids.Count);
		foreach (ulong guid in requestedGuids)
		{
			if (!player.avatarDict.TryGetValue(guid, out var avatar))
			{
				rsp.Retcode = 104;
				session.SendPacket(rsp);
				return;
			}
			requestedAvatars.Add(avatar);
		}
		if (!player.avatarDict.TryGetValue(req.CurAvatarGuid, out var newLeader))
		{
			rsp.Retcode = 104;
			session.SendPacket(rsp);
			return;
		}

		PlayerTeam targetTeam = player.teamList[(int)req.TeamId - 1];
		PlayerAvatar? oldLeader = targetTeam.Leader;
		List<PlayerAvatar> oldAvatars = targetTeam.Avatars.ToList();
		bool isCurrentTeam = req.TeamId == player.TeamIndex;

		targetTeam.Avatars = requestedAvatars;
		targetTeam.Leader = newLeader;
		rsp.AvatarTeamGuidLists.AddRange(requestedGuids);

		TeamHandler.SendAvatarTeamUpdateNotify(session, req.TeamId, requestedGuids);

		if (isCurrentTeam)
		{
			Scene scene = player.Scene;
			// SceneTeam::setPlayerAvatarTeamAndAddToScene foreaches the new
			// SceneTeam and calls Scene::addAvatarAndWeaponEntity for each.
			foreach (var avatar in requestedAvatars)
				scene.AddAvatarAndWeaponEntity(avatar, isEnterScene: true);

			player.SendSceneTeamUpdateNotify(session);
			player.SendSyncTeamEntityNotify(session);

			if (oldLeader != null && oldLeader.Guid != newLeader.Guid)
			{
				// setSceneTeamAndAddToScene delegates a leader change to
				// changeCurAvatar, which replaces Avatar visibility only.
				var disappear = new SceneEntityDisappearNotify { DisappearType = VisionType.VisionReplace };
				disappear.EntityLists.Add(scene.GetOrCreateAvatarEntity(oldLeader)._EntityId);
				session.SendPacket(disappear);

				var appear = new SceneEntityAppearNotify { AppearType = VisionType.VisionReplace };
				appear.EntityLists.Add(scene.GetOrCreateAvatarEntity(newLeader).ToSceneEntityInfo(session));
				session.SendPacket(appear);
			}

			// Scene::delAvatarAndWeaponEntity is used for avatars removed from
			// the active lineup. Unregister them after replacement packets so
			// an old leader is not accidentally recreated just to disappear.
			var requestedSet = requestedGuids.ToHashSet();
			foreach (var removedAvatar in oldAvatars)
				if (!requestedSet.Contains(removedAvatar.Guid))
					scene.RemoveAvatarAndWeaponEntity(removedAvatar);

			TeamHandler.SendAvatarEquipChangeNotify(session, newLeader);
		}

		session.SendPacket(rsp);
		player.SavePersistent();
	}
}
