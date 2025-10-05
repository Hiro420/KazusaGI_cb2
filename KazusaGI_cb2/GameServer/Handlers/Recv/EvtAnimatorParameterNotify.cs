using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtAnimatorParameterNotify
{
    [Packet.PacketCmdId(PacketId.EvtAnimatorParameterNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        EvtAnimatorParameterNotify req = packet.GetDecodedBody<EvtAnimatorParameterNotify>();
        // session.SendPacket(req);
    }
}