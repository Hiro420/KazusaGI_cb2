using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleLogTalkNotify
{
	[Packet.PacketCmdId(PacketId.LogTalkNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		// meant for official server logs, useless for us
	}
}