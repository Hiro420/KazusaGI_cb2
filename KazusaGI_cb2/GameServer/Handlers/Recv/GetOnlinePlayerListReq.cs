using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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