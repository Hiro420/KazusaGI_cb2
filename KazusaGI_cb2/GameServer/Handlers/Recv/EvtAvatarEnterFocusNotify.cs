using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtAvatarEnterFocusNotify
{
    [Packet.PacketCmdId(PacketId.EvtAvatarEnterFocusNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var notify = packet.GetDecodedBody<EvtAvatarEnterFocusNotify>();
        CombatForwarder.Forward(session, notify, notify.ForwardType);
    }
}
