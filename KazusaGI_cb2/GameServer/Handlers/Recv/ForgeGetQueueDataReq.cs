using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleForgeGetQueueDataReq
{
    [Packet.PacketCmdId(PacketId.ForgeGetQueueDataReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        ForgeGetQueueDataReq req = packet.GetDecodedBody<ForgeGetQueueDataReq>(); // maybe implement in future, idk
        ForgeGetQueueDataRsp rsp = new ForgeGetQueueDataRsp();
        session.SendPacket(rsp);
    }
}