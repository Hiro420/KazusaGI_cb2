using KazusaGI_cb2.Enet;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf;
using Serilog;
using System.Numerics;
using System.Reflection;

namespace KazusaGI_cb2.GameServer;

public class Session
{
	public readonly Logger c;
	public Player player;
	public EnetPeer _peer;
	public byte[]? key;
	private ILogger<Session> fileLogger;
	private static readonly string logsFolder = "Logs";
	private static readonly List<string> blacklist = new List<string>() {  // to not flood the console
        "SceneEntityAppearNotify", "AbilityInvocationsNotify", "ClientAbilitiesInitFinishCombineNotify",
		"SceneEntitiesMovesReq", "SceneEntitiesMovesRsp", "EvtAnimatorParameterNotify", "QueryPathReq", "QueryPathRsp",
		"EvtSetAttackTargetNotify", "PlayerStoreNotify", "ClientFpsStatusNotify", "ObstacleModifyNotify",
		"ScenePlayerLocationNotify"
	};
	private JsonSerializer _JsonConverter;
	public bool isMpSession = false;           // todo: change when multiplayer is implemented


	public Session(EnetPeer peer)
	{
		_peer = peer;
		c = new Logger($"Session {_peer}");
		if (Path.Exists(logsFolder))
			Directory.CreateDirectory(logsFolder);
		Log.Logger = new LoggerConfiguration()
				.WriteTo.File(Path.Combine(logsFolder, $"latest_{_peer.IncomingPeerId}.log"), rollingInterval: RollingInterval.Day)
				.CreateLogger();

		// Create logger instance for the session
		fileLogger = LoggerFactory.Create(builder =>
		{
			builder.AddFile(Path.Combine(logsFolder, $"session_{_peer.IncomingPeerId}.log"));
		}).CreateLogger<Session>();

		_JsonConverter = new JsonSerializer
		{
			NullValueHandling = NullValueHandling.Ignore
		};
		_JsonConverter.Converters.Add(new StringEnumConverter());
	}

	public async Task LogToFileAsync(string message)
	{
		await Task.Run(() =>
		{
			fileLogger.LogInformation(message);
		});
	}

	private string PacketToJson(Packet packet)
	{
		try
		{
			PacketId cmd = (PacketId)packet.CmdId;
			string protoName = $"{cmd}";
			Type protoType = Type.GetType($"KazusaGI_cb2.Protocol.{protoName}")!;
			MethodInfo method = typeof(Packet).GetMethod(nameof(packet.GetDecodedBody))!.MakeGenericMethod(protoType);
			string jsonBody = _JsonConverter.SerializeObject(method.Invoke(packet, null)!);
			return jsonBody;
		}
		catch (Exception e)
		{
			c.LogError($"{e.Message}, {e.InnerException}, {e.Source}");
			return String.Empty;
		}
	}

	public SceneEntityInfo CreateSceneEntityInfoFromPlayerAvatar(Session session, PlayerAvatar playerAvatar)
	{
		AvatarEntity avatarEntity = player.Scene.GetOrCreateAvatarEntity(playerAvatar);
		return avatarEntity.ToSceneEntityInfo(session);
	}

	public static Vector3 VectorProto2Vector3(Protocol.Vector vectorProto)
	{
		return new Vector3(vectorProto.X, vectorProto.Y, vectorProto.Z);
	}

	public static Protocol.Vector Vector3ToVector(Vector3 pos)
	{
		return new Protocol.Vector()
		{
			X = pos.X,
			Y = pos.Y,
			Z = pos.Z
		};
	}

	public void onMessage(Packet packet)
	{
		if (packet == null)
		{
			return;
		}
		string protoName = $"{(PacketId)packet.CmdId}";
		string logStr = $"Received {protoName} {PacketToJson(packet)}";
		if (!blacklist.Contains(protoName) && MainApp.config.LogOption.Packets)
			c.LogInfo(logStr);
		LogToFileAsync(logStr).Wait();
		var handler = HandlerFactory.GetHandler((PacketId)packet.CmdId);
		if (handler == null)
		{
			c.LogError($"No handler for {(PacketId)packet.CmdId}");
			return;
		}

		handler?.Invoke(this, packet);
	}


	public bool SendPacket(IExtensible protoMessage)
	{
		try
		{
			string protoName = protoMessage.ToString()!.Split("KazusaGI_cb2.Protocol.").Last();
			PacketId packetId = (PacketId)Enum.Parse(typeof(PacketId), protoName);
			EnetPacket packet = Packet.EncodePacket(this, (ushort)packetId, protoMessage);
			if (_peer.Send(0, packet) != 0)
			{
				packet.Dispose();
				return false;
			}
			string logStr = $"Sent {protoName} {JsonConvert.SerializeObject(protoMessage)}";
			if (!blacklist.Contains(protoName) && MainApp.config.LogOption.Packets)
				c.LogInfo(logStr);
			LogToFileAsync(logStr).Wait();
			return true;
		}
		catch (Exception e)
		{
			c.LogError($"{e.Message}");
			return false;
		}
	}
}
