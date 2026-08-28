using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEntityForceSyncReq
{
	[Packet.PacketCmdId(PacketId.EntityForceSyncReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		var req = packet.GetDecodedBody<EntityForceSyncReq>();

		var rsp = new EntityForceSyncRsp
		{
			EntityId = req.EntityId,
			SceneTime = req.SceneTime,
			Retcode = 0
		};

		var player = session.player;
		if (player?.Scene == null || !player.Scene.TryFindEntity(req.EntityId, out var entity))
		{
			rsp.Retcode = -1;
			session.SendPacket(rsp);
			return;
		}

		var motion = req.MotionInfo;
		if (motion != null)
		{
			entity.SetMotionState(motion.State);
			entity.Position = Session.VectorProto2Vector3(motion.Pos);
			entity.Rotation = Session.VectorProto2Vector3(motion.Rot);
			if (entity is AvatarEntity avatarEntity)
			{
				avatarEntity.DbInfo.LastMoveSceneTimeMs = req.SceneTime;
				avatarEntity.DbInfo.LastMoveParams.Clear();
				avatarEntity.DbInfo.LastMoveParams.AddRange(motion.Params);
			}
		}

		session.SendPacket(rsp);
	}
}