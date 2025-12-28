using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KazusaGI_cb2.Utils;
using System.Threading.Tasks;
using static KazusaGI_cb2.Utils.ENet;
using System.Runtime.InteropServices;
using KazusaGI_cb2.Protocol;

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

        enet_initialize();
        ENetAddress address = new ENetAddress();
        enet_address_set_host(ref address, config.GameServer.ServerIP);
        address.port = Convert.ToUInt16(config.GameServer.ServerPort);
        IntPtr server = enet_host_create(ref address, 999, 0, 0, 0, 0);

        if (server == IntPtr.Zero)
        {
            logger.LogError("An error occurred while trying to create an ENet server host.");
            ENet.enet_deinitialize();
            return;
        }
        enet_host_compress_with_range_coder(server);

        ENet.ENetEvent netEvent;

        logger.LogSuccess($"Staring GameServer on {config.GameServer.ServerIP}:{address.port}");

        while (true)
        {
            //server.CheckEvents(out netEvent);
            enet_host_service(server, out netEvent, 20);


            switch (netEvent.type)
            {
                case ENet.EventType.Connect:
                    logger.LogSuccess($"New connection -> {netEvent.peer}");
                    ENetClient eNetClient = new ENetClient(netEvent.peer);
                    var session = new Session(eNetClient, netEvent.peer);
                    sessions.Add(session);
                    break;
                case ENet.EventType.Disconnect:
                    var _session = sessions.Find(c => c._peer == netEvent.peer)!;
                    logger.LogWarning($"Disconnected {_session._peer}");
                    sessions.Remove(_session);
                    break;
                case ENet.EventType.Receive:
                    ENetPacket enetPacket = Marshal.PtrToStructure<ENetPacket>(netEvent.packet);
                    var session_ = sessions.Find(c => c._peer == netEvent.peer)!;
                    Packet packet = Packet.Read(session_, enetPacket);
                    session_.onMessage(packet);
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
