using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleClientFpsStatusNotify
{
	[Packet.PacketCmdId(PacketId.ClientFpsStatusNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		// no need
	}
}