using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleObstacleModifyNotify
{
	[Packet.PacketCmdId(PacketId.ObstacleModifyNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		// no need
	}
}