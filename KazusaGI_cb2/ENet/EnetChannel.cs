namespace KazusaGI_cb2.Enet;

internal sealed class EnetChannel
{
	public ushort OutgoingReliableSequenceNumber;
	public ushort OutgoingUnreliableSequenceNumber;
	public ushort IncomingReliableSequenceNumber;
	public ushort IncomingUnreliableSequenceNumber;

	public uint UsedReliableWindows;
	public ushort[] ReliableWindows { get; } = new ushort[EnetConstants.ProtocolReliableWindows];

	public LinkedList<IncomingCommand> IncomingReliableCommands { get; } = new();
	public LinkedList<IncomingCommand> IncomingUnreliableCommands { get; } = new();
}

internal sealed class Acknowledgement
{
	public required ushort SentTime;
	public required ProtocolCommandData Command;
}

internal sealed class OutgoingCommand
{
	public ushort ReliableSequenceNumber;
	public ushort UnreliableSequenceNumber;
	public uint SentTime;
	public uint RoundTripTimeout;
	public uint QueueTime;
	public uint FragmentOffset;
	public ushort FragmentLength;
	public ushort SendAttempts;
	public required ProtocolCommandData Command;
	public EnetPacket? Packet;
}

internal sealed class IncomingCommand
{
	public ushort ReliableSequenceNumber;
	public ushort UnreliableSequenceNumber;
	public required ProtocolCommandData Command;
	public uint FragmentCount;
	public uint FragmentsRemaining;
	public uint[]? Fragments;
	public EnetPacket? Packet;
}
