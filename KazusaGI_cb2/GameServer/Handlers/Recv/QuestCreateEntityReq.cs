using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleQuestCreateEntityReq
{
	[Packet.PacketCmdId(PacketId.QuestCreateEntityReq)]
	public static void OnPacket(Session session, Packet packet)
	{
		// maybe later for quests ???
	}
}