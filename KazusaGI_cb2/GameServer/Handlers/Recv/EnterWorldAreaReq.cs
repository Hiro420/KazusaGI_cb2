using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEnterWorldAreaReq
{
    [Packet.PacketCmdId(PacketId.EnterWorldAreaReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        EnterWorldAreaReq req = packet.GetDecodedBody<EnterWorldAreaReq>();
        EnterWorldAreaRsp rsp = new EnterWorldAreaRsp()
        {
            AreaId = req.AreaId,
            AreaType = req.AreaType
        };
        session.SendPacket(rsp);
    }
}