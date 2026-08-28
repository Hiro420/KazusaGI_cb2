using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEnterSceneReadyReq
{
	[Packet.PacketCmdId(PacketId.EnterSceneReadyReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		EnterSceneReadyReq req = packet.GetDecodedBody<EnterSceneReadyReq>();

		// Mirror hk4e: validate enter_scene_token for the ready step.
		if (req.EnterSceneToken != session.player!.EnterSceneToken)
		{
			session.SendPacket(new EnterSceneReadyRsp
			{
				Retcode = (int)Retcode.RetEnterSceneTokenInvalid
			});
			return;
		}

		EnterScenePeerNotify rsp = new EnterScenePeerNotify()
		{
			PeerId = session.player!.PeerId,
			HostPeerId = session.player!.PeerId,
			DestSceneId = session.player!.SceneId
		};

		session.SendPacket(rsp);
		session.SendPacket(new EnterSceneReadyRsp { Retcode = 0 });
	}
}