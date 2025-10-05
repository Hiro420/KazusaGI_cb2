using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleObstacleModifyNotify
{
    [Packet.PacketCmdId(PacketId.ObstacleModifyNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        // no need
    }
}