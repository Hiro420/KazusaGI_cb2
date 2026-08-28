using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtDestroyGadgetNotify
{
	[Packet.PacketCmdId(PacketId.EvtDestroyGadgetNotify)]
	public static void OnPacket(Session session, Packet packet)
	{
		EvtDestroyGadgetNotify req = packet.GetDecodedBody<EvtDestroyGadgetNotify>();
		uint entityId = req.EntityId;
		if (session.player?.Scene == null)
		{
			return;
		}

		if (!session.player.Scene.TryFindEntity(entityId, out var entity))
		{
			return;
		}

		if (entity is GadgetEntity)
		{
			session.player.Scene.EntityManager.Remove(entityId);
		}
		else
		{
			session.c.LogWarning($"EvtDestroyGadgetNotify for non-gadget entity {entityId} ({entity.GetType().Name}); ignoring.");
		}
	}
}