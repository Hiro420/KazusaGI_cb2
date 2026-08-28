using System.Net;
using System.Net.Sockets;

namespace KazusaGI_cb2.Enet;

public sealed partial class EnetHost : IDisposable
{
	private readonly Socket _socket;
	private readonly EnetPeer[] _peers;
	private readonly LinkedList<EnetPeer> _dispatchQueue = new();
	private readonly byte[] _receiveBuffer = new byte[EnetConstants.HostReceiveBufferLength];
	private readonly byte[] _packetBuffer = new byte[EnetConstants.ProtocolMaximumMtu * 2];
	private bool _disposed;
	private uint _randomSeed;
	private uint _bandwidthThrottleEpoch;
	private uint _totalQueued;
	private bool _recalculateBandwidthLimits;
	private EnetRangeCoder? _rangeCoder;

	public EnetHost(
		EnetAddress? address,
		int peerCount,
		int channelLimit = EnetConstants.ProtocolMaximumChannelCount,
		uint incomingBandwidth = 0,
		uint outgoingBandwidth = 0)
	{
		if ((uint)peerCount > EnetConstants.ProtocolMaximumPeerId)
			throw new ArgumentOutOfRangeException(nameof(peerCount));
		if (peerCount < 0)
			throw new ArgumentOutOfRangeException(nameof(peerCount));

		if (channelLimit < 1 || channelLimit > EnetConstants.ProtocolMaximumChannelCount)
			channelLimit = EnetConstants.ProtocolMaximumChannelCount;

		IncomingBandwidth = incomingBandwidth;
		OutgoingBandwidth = outgoingBandwidth;
		ChannelLimit = channelLimit;
		Mtu = EnetConstants.HostDefaultMtu;
		MaximumPacketSize = EnetConstants.HostDefaultMaximumPacketSize;
		MaximumWaitingData = EnetConstants.HostDefaultMaximumWaitingData;
		RetransmissionBackoff = EnetConstants.HostRetransmissionBackoff;
		DuplicatePeers = EnetConstants.ProtocolMaximumPeerId;
		// KazusaGI's stable Windows enet.dll installs enet_crc32 directly in
		// enet_host_create; compression is still enabled separately by the caller.
		Checksum = static data => EnetCrc32.Compute(data);

		_randomSeed = RotateLeft(unchecked((uint)GetHashCode()) + ENet.HostRandomSeed(), 16);

		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp)
		{
			Blocking = false,
			EnableBroadcast = true,
			ReceiveBufferSize = EnetConstants.HostReceiveBufferSize,
			SendBufferSize = EnetConstants.HostSendBufferSize,
		};

		try
		{
			if (address is { } bindAddress)
			{
				_socket.Bind(bindAddress.ToEndPoint());
				Address = EnetAddress.FromEndPoint(_socket.LocalEndPoint!);
			}
			else
			{
				Address = new EnetAddress(IPAddress.Any, 0);
			}
		}
		catch
		{
			_socket.Dispose();
			throw;
		}

		_peers = new EnetPeer[peerCount];
		for (ushort i = 0; i < peerCount; i++)
			_peers[i] = new EnetPeer(this, i);
	}

	public EnetAddress Address { get; private set; }
	public IReadOnlyList<EnetPeer> Peers => _peers;
	public int ChannelLimit { get; private set; }
	public uint IncomingBandwidth { get; private set; }
	public uint OutgoingBandwidth { get; private set; }
	public int Mtu { get; private set; }
	public int MaximumPacketSize { get; set; }
	public long MaximumWaitingData { get; set; }
	public float RetransmissionBackoff { get; set; }
	public int DuplicatePeers { get; set; }
	public ulong TotalSentData { get; private set; }
	public ulong TotalSentPackets { get; private set; }
	public ulong TotalReceivedData { get; private set; }
	public ulong TotalReceivedPackets { get; private set; }
	public long ConnectedPeers { get; private set; }
	public long BandwidthLimitedPeers { get; private set; }
	public uint ServiceTime { get; private set; }

	/// <summary>
	/// Optional packet checksum. For this CB1 build the checksum input contains
	/// the wire header, a connectId/zero seed in the checksum slot, and the
	/// uncompressed command stream (even when the transmitted body is compressed).
	/// Return the native 32-bit value to place in the little-endian wire slot.
	/// </summary>
	public Func<ReadOnlyMemory<byte>, uint>? Checksum { get; set; }

	/// <summary>Enables the exact adaptive range coder used by this build.</summary>
	public void CompressWithRangeCoder() => _rangeCoder = new EnetRangeCoder();
	public void DisableCompression() => _rangeCoder = null;
	public void EnableCrc32Checksum() => Checksum = static data => EnetCrc32.Compute(data);
	public void DisableChecksum() => Checksum = null;

	public void SetChannelLimit(int channelLimit)
	{
		if (channelLimit < 1 || channelLimit > EnetConstants.ProtocolMaximumChannelCount)
			channelLimit = EnetConstants.ProtocolMaximumChannelCount;
		ChannelLimit = channelLimit;
	}

	public void SetBandwidthLimit(uint incomingBandwidth, uint outgoingBandwidth)
	{
		IncomingBandwidth = incomingBandwidth;
		OutgoingBandwidth = outgoingBandwidth;
		_recalculateBandwidthLimits = true;
	}

	public EnetPeer? Connect(EnetAddress address, int channelCount, uint data = 0)
	{
		ThrowIfDisposed();
		EnetPeer? peer = _peers.FirstOrDefault(p => p.State == EnetPeerState.Disconnected);
		if (peer is null) return null;

		if (channelCount < EnetConstants.ProtocolMinimumChannelCount)
			channelCount = EnetConstants.ProtocolMinimumChannelCount;
		else if (channelCount > EnetConstants.ProtocolMaximumChannelCount)
			channelCount = EnetConstants.ProtocolMaximumChannelCount;

		peer.Channels = CreateChannels(channelCount);
		peer.State = EnetPeerState.Connecting;
		peer.Address = address;
		peer.ConnectId = unchecked(++_randomSeed); // exact attached build behavior
		peer.Mtu = (uint)Mtu;

		if (OutgoingBandwidth == 0)
		{
			peer.WindowSize = EnetConstants.ProtocolMaximumWindowSize;
		}
		else
		{
			uint window = (OutgoingBandwidth / EnetConstants.PeerWindowSizeScale)
						  * EnetConstants.ProtocolMinimumWindowSize;
			peer.WindowSize = Math.Clamp(window,
				EnetConstants.ProtocolMinimumWindowSize,
				EnetConstants.ProtocolMaximumWindowSize);
		}

		QueueOutgoingCommand(peer, new ProtocolCommandData
		{
			Command = (byte)((byte)EnetProtocolCommand.Connect | EnetConstants.ProtocolCommandFlagAcknowledge),
			ChannelId = 0xFF,
			OutgoingPeerId = peer.IncomingPeerId,
			IncomingSessionId = peer.IncomingSessionId,
			OutgoingSessionId = peer.OutgoingSessionId,
			Mtu = peer.Mtu,
			WindowSize = peer.WindowSize,
			ChannelCount = (uint)channelCount,
			IncomingBandwidth = IncomingBandwidth,
			OutgoingBandwidth = OutgoingBandwidth,
			PacketThrottleInterval = peer.PacketThrottleInterval,
			PacketThrottleAcceleration = peer.PacketThrottleAcceleration,
			PacketThrottleDeceleration = peer.PacketThrottleDeceleration,
			ConnectId = peer.ConnectId,
			Data = data,
		}, null, 0, 0);

		return peer;
	}

	private static EnetChannel[] CreateChannels(int channelCount)
	{
		var result = new EnetChannel[channelCount];
		for (int i = 0; i < channelCount; i++)
			result[i] = new EnetChannel();
		return result;
	}

	public void Broadcast(byte channelId, EnetPacket packet)
	{
		ArgumentNullException.ThrowIfNull(packet);
		foreach (var peer in _peers)
			if (peer.State == EnetPeerState.Connected)
				peer.Send(channelId, packet);

		if (packet.ReferenceCount == 0)
			packet.Dispose();
	}

	internal int QueuePacket(EnetPeer peer, byte channelId, EnetPacket packet)
	{
		EnetChannel channel = peer.Channels[channelId];
		int checksumOverhead = Checksum is null ? 0 : 4;
		int fragmentLength = checked((int)peer.Mtu) - 28 - checksumOverhead;
		if (fragmentLength <= 0) return -1;

		if (packet.Length <= fragmentLength)
		{
			ProtocolCommandData command;
			if ((packet.Flags & (EnetPacketFlags.Reliable | EnetPacketFlags.Unsequenced)) == EnetPacketFlags.Unsequenced)
			{
				command = new ProtocolCommandData
				{
					Command = (byte)((byte)EnetProtocolCommand.SendUnsequenced | EnetConstants.ProtocolCommandFlagUnsequenced),
					ChannelId = channelId,
					DataLength = checked((ushort)packet.Length),
				};
			}
			else if ((packet.Flags & EnetPacketFlags.Reliable) != 0 || channel.OutgoingUnreliableSequenceNumber == ushort.MaxValue)
			{
				command = new ProtocolCommandData
				{
					Command = (byte)((byte)EnetProtocolCommand.SendReliable | EnetConstants.ProtocolCommandFlagAcknowledge),
					ChannelId = channelId,
					DataLength = checked((ushort)packet.Length),
				};
			}
			else
			{
				command = new ProtocolCommandData
				{
					Command = (byte)EnetProtocolCommand.SendUnreliable,
					ChannelId = channelId,
					DataLength = checked((ushort)packet.Length),
				};
			}

			return QueueOutgoingCommand(peer, command, packet, 0, checked((ushort)packet.Length)) is null ? -1 : 0;
		}

		uint fragmentCount = checked((uint)((packet.Length + fragmentLength - 1) / fragmentLength));
		if (fragmentCount > EnetConstants.ProtocolMaximumFragmentCount)
			return -1;

		bool unreliableFragments = ShouldUseUnreliableFragments(
			packet.Flags, channel.OutgoingUnreliableSequenceNumber);
		byte rawCommand = unreliableFragments
			? (byte)EnetProtocolCommand.SendUnreliableFragment
			: (byte)((byte)EnetProtocolCommand.SendFragment | EnetConstants.ProtocolCommandFlagAcknowledge);
		ushort startSequence = unreliableFragments
			? unchecked((ushort)(channel.OutgoingUnreliableSequenceNumber + 1))
			: unchecked((ushort)(channel.OutgoingReliableSequenceNumber + 1));

		int offset = 0;
		for (uint fragmentNumber = 0; fragmentNumber < fragmentCount; fragmentNumber++)
		{
			int remaining = packet.Length - offset;
			ushort length = checked((ushort)Math.Min(fragmentLength, remaining));
			var command = new ProtocolCommandData
			{
				Command = rawCommand,
				ChannelId = channelId,
				StartSequenceNumber = startSequence,
				DataLength = length,
				FragmentCount = fragmentCount,
				FragmentNumber = fragmentNumber,
				TotalLength = checked((uint)packet.Length),
				FragmentOffset = checked((uint)offset),
			};

			if (QueueOutgoingCommand(peer, command, packet, checked((uint)offset), length) is null)
				return -1;
			offset += length;
		}

		return 0;
	}

	internal OutgoingCommand? QueueOutgoingCommand(
		EnetPeer peer,
		ProtocolCommandData command,
		EnetPacket? packet,
		uint fragmentOffset,
		ushort fragmentLength)
	{
		var outgoing = new OutgoingCommand
		{
			Command = command.Clone(),
			Packet = packet,
			FragmentOffset = fragmentOffset,
			FragmentLength = fragmentLength,
		};

		SetupOutgoingCommand(peer, outgoing);
		if (packet is not null)
			packet.ReferenceCount++;
		return outgoing;
	}

	private void SetupOutgoingCommand(EnetPeer peer, OutgoingCommand outgoing)
	{
		ProtocolCommandData command = outgoing.Command;
		peer.OutgoingDataTotal = unchecked(peer.OutgoingDataTotal
			+ (uint)(ProtocolCodec.GetCommandSize(command.Command) + outgoing.FragmentLength));
		byte channelId = command.ChannelId;
		ushort reliable;
		ushort unreliable = 0;

		if (channelId == 0xFF)
		{
			reliable = unchecked(++peer.OutgoingReliableSequenceNumber);
			outgoing.ReliableSequenceNumber = reliable;
		}
		else
		{
			EnetChannel channel = peer.Channels[channelId];
			if ((command.Command & EnetConstants.ProtocolCommandFlagAcknowledge) != 0)
			{
				channel.OutgoingUnreliableSequenceNumber = 0;
				reliable = unchecked(++channel.OutgoingReliableSequenceNumber);
				outgoing.ReliableSequenceNumber = reliable;
			}
			else if ((command.Command & EnetConstants.ProtocolCommandFlagUnsequenced) != 0)
			{
				peer.OutgoingUnsequencedGroup = unchecked((ushort)(peer.OutgoingUnsequencedGroup + 1));
				reliable = 0;
				outgoing.ReliableSequenceNumber = 0;
			}
			else
			{
				unreliable = channel.OutgoingUnreliableSequenceNumber;
				if (outgoing.FragmentOffset == 0)
					unreliable = ++channel.OutgoingUnreliableSequenceNumber;
				reliable = channel.OutgoingReliableSequenceNumber;
				outgoing.UnreliableSequenceNumber = unreliable;
				outgoing.ReliableSequenceNumber = reliable;
			}
		}

		command.ReliableSequenceNumber = reliable;
		if (command.Kind == EnetProtocolCommand.SendUnreliable)
			command.UnreliableSequenceNumber = unreliable;
		else if (command.Kind == EnetProtocolCommand.SendUnsequenced)
			command.UnsequencedGroup = peer.OutgoingUnsequencedGroup;

		// enet.dll 1.3.18 stores a monotonically increasing queue serial in
		// every outgoing command (host.totalQueued + 1). The send routine
		// merges its two lists using this value, including across uint wrap.
		outgoing.QueueTime = unchecked(++_totalQueued);

		// Exact stable-DLL split (sub_180008110): only ACK-required commands
		// WITH a packet go to outgoingSendReliableCommands (+0x100). Reliable
		// control commands and every non-ACK command go to outgoingCommands
		// (+0x110). The old managed port incorrectly treated the lists as
		// simply reliable vs unreliable.
		if (UsesSendReliableQueue(command.Command, outgoing.Packet is not null))
			peer.OutgoingReliableCommands.AddLast(outgoing);
		else
			peer.OutgoingUnreliableCommands.AddLast(outgoing);
	}

	internal static bool UsesSendReliableQueue(byte command, bool hasPacket)
		=> (command & EnetConstants.ProtocolCommandFlagAcknowledge) != 0 && hasPacket;

	internal static bool ShouldUseUnreliableFragments(EnetPacketFlags flags, ushort outgoingUnreliableSequence)
		=> (flags & (EnetPacketFlags.Reliable | EnetPacketFlags.UnreliableFragment))
			   == EnetPacketFlags.UnreliableFragment
		   && outgoingUnreliableSequence != ushort.MaxValue;

	public void Flush()
	{
		ThrowIfDisposed();
		ServiceTime = ENet.TimeGet();
		SendOutgoingCommands(null, checkTimeouts: false);
	}

	internal void DisconnectNow(EnetPeer peer, uint data)
	{
		if (peer.State == EnetPeerState.Disconnected) return;
		peer.PendingDisconnectReason = "local-now";
		if (peer.State is not EnetPeerState.Zombie and not EnetPeerState.Disconnecting)
		{
			peer.ResetQueues();
			QueueOutgoingCommand(peer, new ProtocolCommandData
			{
				Command = (byte)((byte)EnetProtocolCommand.Disconnect | EnetConstants.ProtocolCommandFlagUnsequenced),
				ChannelId = 0xFF,
				Data = data,
			}, null, 0, 0);
			Flush();
		}
		peer.Reset();
	}

	internal void Disconnect(EnetPeer peer, uint data)
	{
		if (peer.State is EnetPeerState.Disconnected or EnetPeerState.Zombie or EnetPeerState.Disconnecting)
			return;

		peer.PendingDisconnectReason = "local";
		peer.ResetQueues();
		bool connected = peer.State is EnetPeerState.Connected or EnetPeerState.DisconnectLater;
		QueueOutgoingCommand(peer, new ProtocolCommandData
		{
			Command = connected
				? (byte)((byte)EnetProtocolCommand.Disconnect | EnetConstants.ProtocolCommandFlagAcknowledge)
				: (byte)((byte)EnetProtocolCommand.Disconnect | EnetConstants.ProtocolCommandFlagUnsequenced),
			ChannelId = 0xFF,
			Data = data,
		}, null, 0, 0);

		if (connected)
		{
			OnPeerDisconnect(peer);
			peer.State = EnetPeerState.Disconnecting;
		}
		else
		{
			Flush();
			peer.Reset();
		}
	}

	internal void DisconnectLater(EnetPeer peer, uint data)
	{
		if (peer.State is not (EnetPeerState.Connected or EnetPeerState.DisconnectLater))
		{
			Disconnect(peer, data);
			return;
		}

		if (peer.OutgoingReliableCommands.Count == 0
			&& peer.OutgoingUnreliableCommands.Count == 0
			&& peer.SentReliableCommands.Count == 0)
		{
			Disconnect(peer, data);
			return;
		}

		peer.State = EnetPeerState.DisconnectLater;
		peer.EventData = data;
	}

	internal void OnPeerConnect(EnetPeer peer)
	{
		if (peer.State is EnetPeerState.Connected or EnetPeerState.DisconnectLater)
			return;
		ConnectedPeers++;
		if (peer.IncomingBandwidth != 0)
			BandwidthLimitedPeers++;
	}

	internal void OnPeerDisconnect(EnetPeer peer)
	{
		if (peer.State is not (EnetPeerState.Connected or EnetPeerState.DisconnectLater))
			return;
		ConnectedPeers--;
		if (peer.IncomingBandwidth != 0)
			BandwidthLimitedPeers--;
	}

	internal void QueueDispatch(EnetPeer peer)
	{
		if (peer.NeedsDispatch) return;
		_dispatchQueue.AddLast(peer);
		peer.NeedsDispatch = true;
	}

	internal void RemoveDispatch(EnetPeer peer)
	{
		var node = _dispatchQueue.Find(peer);
		if (node is not null) _dispatchQueue.Remove(node);
	}

	private static uint RotateLeft(uint value, int shift) => (value << shift) | (value >> (32 - shift));

	private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		foreach (var peer in _peers)
			peer.Reset();
		_socket.Dispose();
		GC.SuppressFinalize(this);
	}
}
