using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetPlayerMpModeAvailabilityReq
{
    [Packet.PacketCmdId(PacketId.GetPlayerMpModeAvailabilityReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        GetPlayerMpModeAvailabilityReq req = packet.GetDecodedBody<GetPlayerMpModeAvailabilityReq>();
        GetPlayerMpModeAvailabilityRsp rsp = new GetPlayerMpModeAvailabilityRsp();
        session.SendPacket(rsp);
    }
}