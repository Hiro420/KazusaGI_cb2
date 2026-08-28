using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleTowerEnterLevelReq
{
	[Packet.PacketCmdId(PacketId.TowerEnterLevelReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		TowerEnterLevelReq towerEnterLevelReq = packet.GetDecodedBody<TowerEnterLevelReq>();
		if (session.player!.towerInstance == null)
		{
			session.c.LogError("TowerInstance is null");
			session.SendPacket(new TowerEnterLevelRsp()
			{
				Retcode = -1,
			});
			return;
		}
		session.player.towerInstance.HandleTowerEnterLevelReq(packet);
	}
}