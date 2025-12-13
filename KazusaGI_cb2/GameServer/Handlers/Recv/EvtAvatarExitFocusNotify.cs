using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtAvatarExitFocusNotify
{
    [Packet.PacketCmdId(PacketId.EvtAvatarExitFocusNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var notify = packet.GetDecodedBody<EvtAvatarExitFocusNotify>();
        CombatForwarder.Forward(session, notify, notify.ForwardType);
    }
}
