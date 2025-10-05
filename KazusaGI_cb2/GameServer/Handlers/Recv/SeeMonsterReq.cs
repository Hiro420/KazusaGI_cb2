using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSeeMonsterReq
{
    [Packet.PacketCmdId(PacketId.SeeMonsterReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        SeeMonsterReq req = packet.GetDecodedBody<SeeMonsterReq>();
        SeeMonsterRsp rsp = new SeeMonsterRsp();
        session.SendPacket(rsp);
    }
}