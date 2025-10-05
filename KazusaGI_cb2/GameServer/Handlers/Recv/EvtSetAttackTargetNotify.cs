using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtSetAttackTargetNotify
{
    [Packet.PacketCmdId(PacketId.EvtSetAttackTargetNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        // do nothing
    }
}