using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleQueryPathReq
{
    [Packet.PacketCmdId(PacketId.QueryPathReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        QueryPathReq req = packet.GetDecodedBody<QueryPathReq>();
        QueryPathRsp rsp = new QueryPathRsp();
        if (req.DestinationPos.Count > 0)
        {
            rsp.Corners.Add(req.SourcePos);
            rsp.Corners.Add(req.DestinationPos[0]);
            rsp.QueryId = req.QueryId;
            rsp.QueryStatus = QueryPathRsp.PathStatusType.StatusSucc;
        }
        session.SendPacket(rsp);
    }
}