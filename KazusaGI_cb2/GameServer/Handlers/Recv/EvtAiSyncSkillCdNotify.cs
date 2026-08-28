using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtAiSyncSkillCdNotify
{
	[Packet.PacketCmdId(PacketId.EvtAiSyncSkillCdNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		var notify = packet.GetDecodedBody<EvtAiSyncSkillCdNotify>();

		CombatForwarder.Forward(session, notify, ForwardType.ForwardToAllExceptCur);
	}
}