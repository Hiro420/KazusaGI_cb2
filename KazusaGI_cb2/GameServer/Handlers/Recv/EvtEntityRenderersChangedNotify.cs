using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtEntityRenderersChangedNotify
{
    [Packet.PacketCmdId(PacketId.EvtEntityRenderersChangedNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        EvtEntityRenderersChangedNotify req = packet.GetDecodedBody<EvtEntityRenderersChangedNotify>();
        // session.SendPacket(req);
    }
}