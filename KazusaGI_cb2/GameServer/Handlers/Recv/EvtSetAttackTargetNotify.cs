using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtSetAttackTargetNotify
{
	[Packet.PacketCmdId(PacketId.EvtSetAttackTargetNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		var notify = packet.GetDecodedBody<EvtSetAttackTargetNotify>();

		CombatForwarder.Forward(session, notify, notify.ForwardType);
	}
}