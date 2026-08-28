namespace KazusaGI_cb2.Enet;

public static class EnetConstants
{
	// KazusaGI's stable Windows enet.dll reports ENet 1.3.18.
	public const uint LinkedVersion = 0x010312;

	public const int ProtocolMinimumMtu = 576;
	public const int ProtocolMaximumMtu = 4096;
	public const int HostDefaultMtu = 1392; // stable Windows enet.dll host_create stores 0x570
	public const int HostReceiveBufferSize = 256 * 1024;
	public const int HostSendBufferSize = 256 * 1024;
	public const int HostReceiveBufferLength = 4096;
	public const int HostDefaultMaximumPacketSize = 32 * 1024 * 1024;
	public const long HostDefaultMaximumWaitingData = 32L * 1024 * 1024; // host+0x2B08 in supplied ELF
	public const float HostRetransmissionBackoff = 2.0f; // trailing 0x40000000 IEEE-754 field

	public const int ProtocolMaximumPeerId = 0x0FFF;
	public const int ProtocolMaximumChannelCount = 255;
	public const int ProtocolMinimumChannelCount = 1;
	public const int ProtocolMaximumWindowSize = 65536;
	public const int ProtocolMinimumWindowSize = 4096;
	public const int ProtocolMinimumWindowSizeScale = 64 * 1024;

	public const int PeerDefaultRoundTripTime = 500;
	public const int PeerDefaultPacketThrottle = 32;
	public const int PeerPacketThrottleScale = 32;
	public const int PeerPacketThrottleCounter = 7;
	public const int PeerPacketThrottleAcceleration = 2;
	public const int PeerPacketThrottleDeceleration = 2;
	public const int PeerPacketThrottleInterval = 5000;
	public const int PeerPingInterval = 500;
	public const int PeerTimeoutLimit = 32;
	public const int PeerTimeoutMinimum = 5000;
	public const int PeerTimeoutMaximum = 30000;
	public const int PeerWindowSizeScale = 64 * 1024;
	public const int PeerPacketLossScale = 1 << 16;
	public const int PeerPacketLossInterval = 10000;

	public const int ProtocolReliableWindowSize = 0x1000;
	public const int ProtocolReliableWindows = 16;
	public const int ProtocolFreeReliableWindows = 8;
	public const int ProtocolMaximumFragmentCount = 1024 * 1024;
	public const int ProtocolMaximumPacketCommands = 32;
	public const int ProtocolMaximumHeaderSize = 8;
	public const int ProtocolMaximumCommandSize = 48;

	public const ushort ProtocolHeaderFlagSentTime = 1 << 15;
	public const ushort ProtocolHeaderFlagCompressed = 1 << 14;
	public const ushort ProtocolHeaderFlagMask = ProtocolHeaderFlagSentTime | ProtocolHeaderFlagCompressed;
	public const ushort ProtocolHeaderSessionMask = 3 << 12;
	public const int ProtocolHeaderSessionShift = 12;
	public const ushort ProtocolHeaderPeerIdMask = 0x0FFF;

	public const byte ProtocolCommandMask = 0x0F;
	public const byte ProtocolCommandFlagAcknowledge = 1 << 7;
	public const byte ProtocolCommandFlagUnsequenced = 1 << 6;

	public const int TimeOverflow = 86400000;
	public const int BandwidthThrottleInterval = 1000;
}

public enum EnetPeerState
{
	Disconnected = 0,
	Connecting = 1,
	AcknowledgingConnect = 2,
	ConnectionPending = 3,
	ConnectionSucceeded = 4,
	Connected = 5,
	DisconnectLater = 6,
	Disconnecting = 7,
	AcknowledgingDisconnect = 8,
	Zombie = 9,
}

public enum EnetEventType
{
	None = 0,
	Connect = 1,
	Disconnect = 2,
	Receive = 3,
}

[Flags]
public enum EnetPacketFlags : uint
{
	None = 0,
	Reliable = 1 << 0,
	Unsequenced = 1 << 1,
	NoAllocate = 1 << 2,
	UnreliableFragment = 1 << 3,
	Sent = 1 << 8,
}

internal enum EnetProtocolCommand : byte
{
	None = 0,
	Acknowledge = 1,
	Connect = 2,
	VerifyConnect = 3,
	Disconnect = 4,
	Ping = 5,
	SendReliable = 6,
	SendUnreliable = 7,
	SendFragment = 8,
	SendUnsequenced = 9,
	BandwidthLimit = 10,
	ThrottleConfigure = 11,
	SendUnreliableFragment = 12,
	Count = 13,
}
