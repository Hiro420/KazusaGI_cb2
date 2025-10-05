using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePingReq
{
    [Packet.PacketCmdId(PacketId.PingReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        PingReq req = packet.GetDecodedBody<PingReq>();
        PingRsp rsp = new PingRsp()
        {
            ClientTime = req.ClientTime,
            Seq = req.Seq,
        };
        session.SendPacket(rsp);
    }
}