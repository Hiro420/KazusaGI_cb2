using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtAvatarUpdateFocusNotify
{
    [Packet.PacketCmdId(PacketId.EvtAvatarUpdateFocusNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var notify = packet.GetDecodedBody<EvtAvatarUpdateFocusNotify>();
        CombatForwarder.Forward(session, notify, notify.ForwardType);
    }
}
