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
        var notify = packet.GetDecodedBody<EvtEntityRenderersChangedNotify>();
        if (notify.IsServerCache && session.player?.Scene != null &&
            session.player.Scene.EntityManager.TryGet(notify.EntityId, out var entity))
        {
            entity.CachedRendererChangedInfo = notify.RendererChangedInfo;
        }

        CombatForwarder.Forward(session, notify, notify.ForwardType);
    }
}