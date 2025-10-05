using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleClientFpsStatusNotify
{
    [Packet.PacketCmdId(PacketId.ClientFpsStatusNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        // no need
    }
}