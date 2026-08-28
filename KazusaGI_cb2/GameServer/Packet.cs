using KazusaGI_cb2.Enet;
using KazusaGI_cb2.Protocol;
using ProtoBuf;
using System.Net;
using System.Reflection;
using static KazusaGI_cb2.Utils.Crypto;

namespace KazusaGI_cb2.GameServer;

public class Packet
{
	public uint CmdId { get; set; }
	public byte[] FinishedBody { get; set; }
	public IExtensible Ori { get; set; } // protobuf-net compatible

	public void SetData<T>(PacketId cmdType, T msg) where T : class, IExtensible
	{
		CmdId = (uint)cmdType;
		FinishedBody = SerializeToByteArray(msg);
		Ori = msg;
	}

	public static byte[] SerializeToByteArray<T>(T obj) where T : class, IExtensible
	{
		using (var ms = new MemoryStream())
		{
			Serializer.Serialize(ms, obj);
			return ms.ToArray();
		}
	}

	public static T DeserializeFromByteArray<T>(byte[] data) where T : class, IExtensible
	{
		using (var ms = new MemoryStream(data))
		{
			return Serializer.Deserialize<T>(ms);
		}
	}

	public T DecodeBody<T>() where T : class, IExtensible, new()
	{
		return DeserializeFromByteArray<T>(FinishedBody);
	}

	public T GetDecodedBody<T>()
	{
		T serializedBody;

		using (MemoryStream stream = new(FinishedBody))
		{
			serializedBody = Serializer.Deserialize<T>(stream);
		}

		return serializedBody;
	}

	public static ushort GetUInt16(byte[] buf, int index)
	{
		ushort networkValue = BitConverter.ToUInt16(buf, index);
		return (ushort)IPAddress.NetworkToHostOrder((short)networkValue);
	}

	public static uint GetUInt32(byte[] buf, int index)
	{
		uint networkValue = BitConverter.ToUInt32(buf, index);
		return (uint)IPAddress.NetworkToHostOrder((int)networkValue);
	}

	public static void PutUInt16(byte[] buf, ushort value, int offset)
	{
		ushort networkValue = (ushort)IPAddress.HostToNetworkOrder((short)value);
		byte[] bytes = BitConverter.GetBytes(networkValue);
		Buffer.BlockCopy(bytes, 0, buf, offset, bytes.Length);
	}

	public static void PutUInt32(byte[] buf, uint value, int offset)
	{
		uint networkValue = (uint)IPAddress.HostToNetworkOrder((int)value);
		byte[] bytes = BitConverter.GetBytes(networkValue);
		Buffer.BlockCopy(bytes, 0, buf, offset, bytes.Length);
	}

	public static void PutByteArray(byte[] destination, byte[] source, int offset)
	{
		Buffer.BlockCopy(source, 0, destination, offset, source.Length);
	}

	public static EnetPacket EncodePacket(Session session, ushort CmdId, IExtensible body)
	{
		using var stream = new MemoryStream();
		PacketHead header = new PacketHead { PacketId = (ushort)CmdId };

		byte[] headerBytes = SerializeToByteArray(header);

		byte[] finishedBody = SerializeToByteArray(body);

		byte[] packetData = new byte[10 + headerBytes.Length + finishedBody.Length + 2];
		PutUInt16(packetData, 0x4567, 0);
		PutUInt16(packetData, (ushort)CmdId, 2);
		PutUInt16(packetData, (ushort)headerBytes.Length, 4);
		PutUInt32(packetData, (ushort)finishedBody.Length, 6);
		PutByteArray(packetData, headerBytes, 10);
		PutByteArray(packetData, finishedBody, 10 + headerBytes.Length);
		PutUInt16(packetData, 0x89AB, 10 + headerBytes.Length + finishedBody.Length);
		if (session.key != null)
		{
			packetData = Xor(packetData, session.key);
		}

		return ENet.PacketCreate(packetData, EnetPacketFlags.Reliable, takeOwnership: true);
	}

	public byte[] ToBytes()
	{
		using var stream = new MemoryStream();
		PacketHead header = new PacketHead { PacketId = (ushort)CmdId };

		// Use reflection to get the type from KazusaGI_cb2.Protocol
		Type protoType = Type.GetType($"KazusaGI_cb2.Protocol.{Ori.GetType().Name}")!;
		if (protoType == null)
			throw new InvalidOperationException($"Proto type for {Ori.GetType().Name} not found.");

		MethodInfo serializeMethod = typeof(ProtoBuf.Serializer).GetMethod("Serialize", new[] { typeof(Stream), protoType })!;
		if (serializeMethod == null)
			throw new InvalidOperationException("Serialize method not found in ProtoBuf.Serializer.");

		serializeMethod.Invoke(null, new object[] { stream, header });
		byte[] headerBytes = stream.ToArray();

		byte[] packetData = new byte[10 + headerBytes.Length + FinishedBody.Length + 2];
		PutUInt16(packetData, 0x4567, 0);
		PutUInt16(packetData, (ushort)CmdId, 2);
		PutUInt16(packetData, (ushort)headerBytes.Length, 4);
		PutUInt32(packetData, (ushort)FinishedBody.Length, 6);
		PutByteArray(packetData, headerBytes, 10);
		PutByteArray(packetData, FinishedBody, 10 + headerBytes.Length);
		PutUInt16(packetData, 0x89AB, 10 + headerBytes.Length + FinishedBody.Length);

		return packetData;
	}

	public static Packet Read(Session session, KazusaGI_cb2.Enet.EnetPacket packet)
	{
		return Read(session, packet.Data);
	}

	public static Packet Read(Session session, byte[] data)
	{
		if (session.key != null)
		{
			data = Xor(data, session.key);
		}
		MemoryStream ms = new MemoryStream(data);
		BinaryReader br = new BinaryReader(ms);
		ushort headerMagic = GetUInt16(data, 0);
		if (headerMagic != 0x4567)
		{
			Console.WriteLine($"invalid header magic. expected: 0x4567, recieved: 0x{headerMagic:X}");
			return null;
		}

		ushort cmdId = GetUInt16(data, 2);
		ushort headerLength = GetUInt16(data, 4);
		uint bodyLength = GetUInt32(data, 6);
		ushort footerMagic = GetUInt16(data, 10 + headerLength + (int)bodyLength);

		if (footerMagic != 0x89AB)
		{
			Console.WriteLine($"invalid footer magic. expected: 0x89AB, recieved: 0x{footerMagic:X}");
			return null;
		}

		byte[] body = new byte[bodyLength];
		Array.Copy(data, 10 + headerLength, body, 0, bodyLength);

		return new Packet { CmdId = cmdId, FinishedBody = body };
	}

	[AttributeUsage(AttributeTargets.Method)]
	public class PacketCmdId : Attribute
	{
		public PacketId Id { get; }

		public PacketCmdId(PacketId id)
		{
			Id = id;
		}
	}
}
