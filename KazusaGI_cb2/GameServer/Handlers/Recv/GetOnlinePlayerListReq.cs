using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetOnlinePlayerListReq
{
	[Packet.PacketCmdId(PacketId.GetOnlinePlayerListReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		GetOnlinePlayerListReq req = packet.GetDecodedBody<GetOnlinePlayerListReq>();
		GetOnlinePlayerListRsp rsp = new GetOnlinePlayerListRsp()
		{
			PlayerInfoLists = { }
		};
		session.SendPacket(rsp);
	}
}