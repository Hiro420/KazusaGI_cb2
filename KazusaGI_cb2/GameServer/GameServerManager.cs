using KazusaGI_cb2.Enet;

namespace KazusaGI_cb2.GameServer;

public class GameServerManager
{
	public static List<Session> sessions = new List<Session>();
	private static uint _lastTimerSecond;
	public static void StartLoop()
	{
		Config config = MainApp.config;
		Logger logger = new("GameServer");

		foreach (var type in typeof(HandlerFactory).Assembly.GetTypes().Where(a => a.FullName!.Contains("KazusaGI_cb2.GameServer.Handlers")))
		{
			foreach (var method in type.GetMethods())
			{
				var attributes = method.GetCustomAttributes(typeof(Packet.PacketCmdId), false);
				if (attributes.Length > 0)
				{
					var attribute = (Packet.PacketCmdId)attributes[0];
					var handler = (Action<Session, Packet>)Delegate.CreateDelegate(typeof(Action<Session, Packet>), method);
					HandlerFactory.RegisterHandler(attribute.Id, handler);
				}
			}
		}

		ENet.Initialize();
		ushort port = Convert.ToUInt16(config.GameServer.ServerPort);
		EnetAddress address = ENet.AddressFromHost(config.GameServer.ServerIP, port);
		EnetHost? server = EnetApi.enet_host_create(address, 999, 0, 0, 0);

		if (server == null)
		{
			logger.LogError("An error occurred while trying to create an ENet server host.");
			ENet.Deinitialize();
			return;
		}
		server.CompressWithRangeCoder();

		EnetEvent netEvent;

		logger.LogSuccess($"Staring GameServer on {address}");

		while (true)
		{
			server.Service(20, out netEvent);


			switch (netEvent.Type)
			{
				case EnetEventType.Connect when netEvent.Peer != null:
					logger.LogSuccess($"New connection -> {netEvent.Peer}");
					var session = new Session(netEvent.Peer);
					sessions.Add(session);
					break;
				case EnetEventType.Disconnect when netEvent.Peer != null:
					var disconnectedSession = sessions.Find(c => c._peer == netEvent.Peer);
					if (disconnectedSession != null)
					{
						logger.LogWarning($"Disconnected {disconnectedSession._peer}");
						sessions.Remove(disconnectedSession);
					}
					break;
				case EnetEventType.Receive when netEvent.Peer != null && netEvent.Packet != null:
					using (netEvent.Packet)
					{
						var receivingSession = sessions.Find(c => c._peer == netEvent.Peer);
						if (receivingSession != null)
						{
							Packet packet = Packet.Read(receivingSession, netEvent.Packet);
							receivingSession.onMessage(packet);
						}
					}
					break;
				default:
					break;
			}

			// Drive gadget OnTimer callbacks roughly once per second using
			// server wall-clock time in seconds, similar to hk4e's gear
			// component timers.
			uint now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			if (now != _lastTimerSecond)
			{
				_lastTimerSecond = now;
				foreach (var s in sessions.ToArray())
				{
					var player = s.player;
					if (player?.Scene != null)
					{
						player.Scene.TickGadgets(now);
					}
				}

				// hk4e calls Scene::notifyAllPlayerLocation periodically but
				// only sends ScenePlayerLocationNotify when there is more
				// than one player in the scene. Mirror that behavior here
				// via Scene.NotifyAllPlayerLocationIfMultiPlayer.
				foreach (var s in sessions.ToArray())
				{
					var player = s.player;
					if (player?.Scene != null)
					{
						player.Scene.NotifyAllPlayerLocationIfMultiPlayer();
					}
				}
			}
		}
	}
}
