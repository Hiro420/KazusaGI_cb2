using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetShopReq
{
	[Packet.PacketCmdId(PacketId.GetShopReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		GetShopReq req = packet.GetDecodedBody<GetShopReq>();
		GetShopRsp rsp = new GetShopRsp()
		{
			Shop = Shop.GetShopByShopType(req.ShopType),
		};
		session.SendPacket(rsp);
	}
}