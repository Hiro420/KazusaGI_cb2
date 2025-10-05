using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleLogTalkNotify
{
    [Packet.PacketCmdId(PacketId.LogTalkNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        // meant for official server logs, useless for us
    }
}