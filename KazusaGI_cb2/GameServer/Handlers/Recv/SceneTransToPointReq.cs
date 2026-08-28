using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSceneTransToPointReq
{
	[Packet.PacketCmdId(PacketId.SceneTransToPointReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		SceneTransToPointReq req = packet.GetDecodedBody<SceneTransToPointReq>();

		ConfigScenePoint scenePoint = MainApp.resourceManager.ScenePoints[req.SceneId].points[req.PointId];

		session.player!.SetRot(scenePoint.tranRot);
		session.player.TeleportToPos(session, scenePoint.tranPos, true);
		session.player.EnterScene(session, req.SceneId, EnterType.EnterGoto);

		SceneTransToPointRsp rsp = new SceneTransToPointRsp()
		{
			PointId = req.PointId,
			SceneId = req.SceneId
		};
		session.SendPacket(rsp);

		session.player!.SavePersistent();
	}
}