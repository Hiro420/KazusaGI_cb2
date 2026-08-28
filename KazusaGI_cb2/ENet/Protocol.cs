using System.Buffers.Binary;

namespace KazusaGI_cb2.Enet;

internal sealed class ProtocolCommandData
{
	public byte Command;
	public byte ChannelId;
	public ushort ReliableSequenceNumber;

	public ushort ReceivedReliableSequenceNumber;
	public ushort ReceivedSentTime;

	public ushort OutgoingPeerId;
	public byte IncomingSessionId;
	public byte OutgoingSessionId;
	public uint Mtu;
	public uint WindowSize;
	public uint ChannelCount;
	public uint IncomingBandwidth;
	public uint OutgoingBandwidth;
	public uint PacketThrottleInterval;
	public uint PacketThrottleAcceleration;
	public uint PacketThrottleDeceleration;
	public uint ConnectId;
	public uint Data;

	public ushort UnreliableSequenceNumber;
	public ushort DataLength;

	public ushort StartSequenceNumber;
	public uint FragmentCount;
	public uint FragmentNumber;
	public uint TotalLength;
	public uint FragmentOffset;

	public ushort UnsequencedGroup;

	public EnetProtocolCommand Kind => (EnetProtocolCommand)(Command & EnetConstants.ProtocolCommandMask);
	public bool RequiresAcknowledgement => (Command & EnetConstants.ProtocolCommandFlagAcknowledge) != 0;
	public bool IsUnsequenced => (Command & EnetConstants.ProtocolCommandFlagUnsequenced) != 0;

	public ProtocolCommandData Clone() => (ProtocolCommandData)MemberwiseClone();
}

internal static class ProtocolCodec
{
	private static readonly int[] CommandSizes =
	[
		0, 8, 48, 44, 8, 4, 6, 8, 24, 8, 12, 16, 24
	];

	public static int GetCommandSize(byte command)
	{
		int index = command & EnetConstants.ProtocolCommandMask;
		return (uint)index < (uint)CommandSizes.Length ? CommandSizes[index] : 0;
	}

	public static bool TryRead(ReadOnlySpan<byte> source, out ProtocolCommandData command, out int bytesRead)
	{
		command = new ProtocolCommandData();
		bytesRead = 0;
		if (source.Length < 4) return false;

		byte raw = source[0];
		int size = GetCommandSize(raw);
		if (size == 0 || source.Length < size) return false;

		command.Command = raw;
		command.ChannelId = source[1];
		command.ReliableSequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(2, 2));

		switch ((EnetProtocolCommand)(raw & EnetConstants.ProtocolCommandMask))
		{
			case EnetProtocolCommand.Acknowledge:
				command.ReceivedReliableSequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
				command.ReceivedSentTime = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(6, 2));
				break;

			case EnetProtocolCommand.Connect:
			case EnetProtocolCommand.VerifyConnect:
				command.OutgoingPeerId = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
				command.IncomingSessionId = source[6];
				command.OutgoingSessionId = source[7];
				command.Mtu = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(8, 4));
				command.WindowSize = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(12, 4));
				command.ChannelCount = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(16, 4));
				command.IncomingBandwidth = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(20, 4));
				command.OutgoingBandwidth = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(24, 4));
				command.PacketThrottleInterval = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(28, 4));
				command.PacketThrottleAcceleration = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(32, 4));
				command.PacketThrottleDeceleration = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(36, 4));
				command.ConnectId = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(40, 4)); // native copies connectID without HOST_TO_NET
				if ((raw & EnetConstants.ProtocolCommandMask) == (byte)EnetProtocolCommand.Connect)
					command.Data = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(44, 4));
				break;

			case EnetProtocolCommand.Disconnect:
				command.Data = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4));
				break;

			case EnetProtocolCommand.SendReliable:
				command.DataLength = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
				break;

			case EnetProtocolCommand.SendUnreliable:
				command.UnreliableSequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
				command.DataLength = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(6, 2));
				break;

			case EnetProtocolCommand.SendFragment:
			case EnetProtocolCommand.SendUnreliableFragment:
				command.StartSequenceNumber = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
				command.DataLength = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(6, 2));
				command.FragmentCount = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(8, 4));
				command.FragmentNumber = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(12, 4));
				command.TotalLength = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(16, 4));
				command.FragmentOffset = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(20, 4));
				break;

			case EnetProtocolCommand.SendUnsequenced:
				command.UnsequencedGroup = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(4, 2));
				command.DataLength = BinaryPrimitives.ReadUInt16BigEndian(source.Slice(6, 2));
				break;

			case EnetProtocolCommand.BandwidthLimit:
				command.IncomingBandwidth = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4));
				command.OutgoingBandwidth = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(8, 4));
				break;

			case EnetProtocolCommand.ThrottleConfigure:
				command.PacketThrottleInterval = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(4, 4));
				command.PacketThrottleAcceleration = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(8, 4));
				command.PacketThrottleDeceleration = BinaryPrimitives.ReadUInt32BigEndian(source.Slice(12, 4));
				break;
		}

		bytesRead = size;
		return true;
	}

	public static int Write(Span<byte> destination, ProtocolCommandData command)
	{
		int size = GetCommandSize(command.Command);
		if (size == 0 || destination.Length < size)
			throw new ArgumentException("Destination is too small for protocol command.", nameof(destination));

		destination.Slice(0, size).Clear();
		destination[0] = command.Command;
		destination[1] = command.ChannelId;
		BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(2, 2), command.ReliableSequenceNumber);

		switch (command.Kind)
		{
			case EnetProtocolCommand.Acknowledge:
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), command.ReceivedReliableSequenceNumber);
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(6, 2), command.ReceivedSentTime);
				break;

			case EnetProtocolCommand.Connect:
			case EnetProtocolCommand.VerifyConnect:
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), command.OutgoingPeerId);
				destination[6] = command.IncomingSessionId;
				destination[7] = command.OutgoingSessionId;
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), command.Mtu);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), command.WindowSize);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(16, 4), command.ChannelCount);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(20, 4), command.IncomingBandwidth);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(24, 4), command.OutgoingBandwidth);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(28, 4), command.PacketThrottleInterval);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(32, 4), command.PacketThrottleAcceleration);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(36, 4), command.PacketThrottleDeceleration);
				BinaryPrimitives.WriteUInt32LittleEndian(destination.Slice(40, 4), command.ConnectId); // native copies connectID without HOST_TO_NET
				if (command.Kind == EnetProtocolCommand.Connect)
					BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(44, 4), command.Data);
				break;

			case EnetProtocolCommand.Disconnect:
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), command.Data);
				break;

			case EnetProtocolCommand.SendReliable:
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), command.DataLength);
				break;

			case EnetProtocolCommand.SendUnreliable:
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), command.UnreliableSequenceNumber);
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(6, 2), command.DataLength);
				break;

			case EnetProtocolCommand.SendFragment:
			case EnetProtocolCommand.SendUnreliableFragment:
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), command.StartSequenceNumber);
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(6, 2), command.DataLength);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), command.FragmentCount);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), command.FragmentNumber);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(16, 4), command.TotalLength);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(20, 4), command.FragmentOffset);
				break;

			case EnetProtocolCommand.SendUnsequenced:
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(4, 2), command.UnsequencedGroup);
				BinaryPrimitives.WriteUInt16BigEndian(destination.Slice(6, 2), command.DataLength);
				break;

			case EnetProtocolCommand.BandwidthLimit:
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), command.IncomingBandwidth);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), command.OutgoingBandwidth);
				break;

			case EnetProtocolCommand.ThrottleConfigure:
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(4, 4), command.PacketThrottleInterval);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(8, 4), command.PacketThrottleAcceleration);
				BinaryPrimitives.WriteUInt32BigEndian(destination.Slice(12, 4), command.PacketThrottleDeceleration);
				break;
		}

		return size;
	}
}
