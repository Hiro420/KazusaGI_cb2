using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtFaceToDirNotify
{
	[Packet.PacketCmdId(PacketId.EvtFaceToDirNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		var notify = packet.GetDecodedBody<EvtFaceToDirNotify>();

		CombatForwarder.Forward(session, notify, notify.ForwardType);
	}
}