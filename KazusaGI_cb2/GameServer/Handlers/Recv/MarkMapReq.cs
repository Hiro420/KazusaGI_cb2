using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleMarkMapReq
{
    [Packet.PacketCmdId(PacketId.MarkMapReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        MarkMapReq req = packet.GetDecodedBody<MarkMapReq>();
        MarkMapRsp rsp = new MarkMapRsp();
        if (req.Mark != null)
        {
            rsp.MarkLists.Add(req.Mark);
        }
        session.SendPacket(rsp);
    }
}