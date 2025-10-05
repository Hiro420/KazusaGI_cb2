using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleMonsterAlertChangeNotify
{
    [Packet.PacketCmdId(PacketId.MonsterAlertChangeNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        // empty
    }
}