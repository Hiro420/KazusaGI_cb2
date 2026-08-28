using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePlayerSetPauseReq
{
	[Packet.PacketCmdId(PacketId.PlayerSetPauseReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		PlayerSetPauseReq req = packet.GetDecodedBody<PlayerSetPauseReq>();
		PlayerSetPauseRsp rsp = new PlayerSetPauseRsp();
		session.SendPacket(rsp);
	}
}