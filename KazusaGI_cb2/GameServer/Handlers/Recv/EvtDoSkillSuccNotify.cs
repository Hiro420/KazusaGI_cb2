using KazusaGI_cb2.Protocol;

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