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
        var notify = packet.GetDecodedBody<EvtAnimatorParameterNotify>();
        var info = notify.AnimatorParamInfo;
        if (info != null && info.IsServerCache && session.player?.Scene != null &&
            session.player.Scene.EntityManager.TryGet(info.EntityId, out var entity))
        {
            entity.AnimatorParameters[info.NameId] = info.Value;
        }

        CombatForwarder.Forward(session, notify, notify.ForwardType);
    }
}