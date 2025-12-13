using System.Linq;
using KazusaGI_cb2.Protocol;
using ProtoBuf;

namespace KazusaGI_cb2.GameServer;

internal static class CombatForwarder
{
	public static void Forward<T>(Session origin, T message, ForwardType forwardType)
		where T : class, IExtensible
	{
		var originPlayer = origin.player;
		if (originPlayer == null)
		{
			return;
		}

		// Currently we only support forwarding within the same scene.
		uint sceneId = originPlayer.SceneId;
		foreach (var session in GameServerManager.sessions.ToList())
		{
			var player = session.player;
			if (player == null || player.SceneId != sceneId)
			{
				continue;
			}

			bool send = forwardType switch
			{
				ForwardType.ForwardLocal => session == origin,
				ForwardType.ForwardToAll => true,
				ForwardType.ForwardToAllExceptCur => session != origin,
				ForwardType.ForwardToAllExistExceptCur => session != origin,
				ForwardType.ForwardToHost => session == origin, // until explicit host tracking exists
				ForwardType.ForwardToAllGuest => session != origin,
				ForwardType.ForwardToPeer => true,
				ForwardType.ForwardToPeers => true,
				ForwardType.ForwardOnlyServer => false,
				_ => false
			};

			if (send)
			{
				session.SendPacket(message);
			}
		}
	}
}
