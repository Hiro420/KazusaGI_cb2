using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtDoSkillSuccNotify
{
    [Packet.PacketCmdId(PacketId.EvtDoSkillSuccNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var notify = packet.GetDecodedBody<EvtDoSkillSuccNotify>();

        CombatForwarder.Forward(session, notify, notify.ForwardType);
    }
}