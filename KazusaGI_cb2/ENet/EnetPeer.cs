namespace KazusaGI_cb2.Enet;

public sealed class EnetPeer
{
	internal EnetPeer(EnetHost host, ushort incomingPeerId)
	{
		Host = host;
		IncomingPeerId = incomingPeerId;
		OutgoingSessionId = 0xFF;
		IncomingSessionId = 0xFF;
		Reset();
	}

	public EnetHost Host { get; }
	public ushort OutgoingPeerId { get; internal set; }
	public ushort IncomingPeerId { get; }
	public uint ConnectId { get; internal set; }
	public byte OutgoingSessionId { get; internal set; }
	public byte IncomingSessionId { get; internal set; }
	public EnetAddress Address { get; internal set; }
	public object? UserData { get; set; }
	public EnetPeerState State { get; internal set; }

	internal EnetChannel[] Channels = Array.Empty<EnetChannel>();
	public int ChannelCount => Channels.Length;

	public uint IncomingBandwidth { get; internal set; }
	public uint OutgoingBandwidth { get; internal set; }
	internal uint IncomingBandwidthThrottleEpoch;
	internal uint OutgoingBandwidthThrottleEpoch;
	internal uint IncomingDataTotal;
	internal uint OutgoingDataTotal;
	internal uint LastSendTime;
	internal uint LastReceiveTime;
	internal uint NextTimeout;
	internal uint EarliestTimeout;
	internal uint PacketLossEpoch;
	internal uint PacketsSent;
	internal uint PacketsLost;
	public uint PacketLoss { get; internal set; }
	public uint PacketLossVariance { get; internal set; }
	public uint PacketThrottle { get; internal set; }
	public uint PacketThrottleLimit { get; internal set; }
	internal uint PacketThrottleCounter;
	internal uint PacketThrottleEpoch;
	public uint PacketThrottleAcceleration { get; internal set; }
	public uint PacketThrottleDeceleration { get; internal set; }
	public uint PacketThrottleInterval { get; internal set; }
	public uint PingInterval { get; private set; }
	public uint TimeoutLimit { get; private set; }
	public uint TimeoutMinimum { get; private set; }
	public uint TimeoutMaximum { get; private set; }
	public uint LastRoundTripTime { get; internal set; }
	public uint LowestRoundTripTime { get; internal set; }
	public uint LastRoundTripTimeVariance { get; internal set; }
	public uint HighestRoundTripTimeVariance { get; internal set; }
	public uint RoundTripTime { get; internal set; }
	public uint RoundTripTimeVariance { get; internal set; }
	public uint Mtu { get; internal set; }
	public uint WindowSize { get; internal set; }
	internal uint ReliableDataInTransit;
	internal ushort OutgoingReliableSequenceNumber;

	internal readonly LinkedList<Acknowledgement> Acknowledgements = new();
	internal readonly LinkedList<OutgoingCommand> SentReliableCommands = new();
	internal readonly LinkedList<OutgoingCommand> SentUnreliableCommands = new();
	internal readonly LinkedList<OutgoingCommand> OutgoingReliableCommands = new();
	internal readonly LinkedList<OutgoingCommand> OutgoingUnreliableCommands = new();
	internal readonly LinkedList<IncomingCommand> DispatchedCommands = new();

	internal bool NeedsDispatch;
	internal ushort IncomingUnsequencedGroup;
	internal ushort OutgoingUnsequencedGroup;
	internal readonly uint[] UnsequencedWindow = new uint[1024 / 32];
	internal uint EventData;
	internal string? PendingDisconnectReason;
	internal long TotalWaitingData;

	public bool IsConnected => State == EnetPeerState.Connected;

	internal void Reset()
	{
		Host.OnPeerDisconnect(this);

		IncomingBandwidth = 0;
		OutgoingBandwidth = 0;
		IncomingBandwidthThrottleEpoch = 0;
		OutgoingBandwidthThrottleEpoch = 0;
		IncomingDataTotal = 0;
		OutgoingDataTotal = 0;
		LastSendTime = 0;
		LastReceiveTime = 0;
		NextTimeout = 0;
		EarliestTimeout = 0;
		PacketLossEpoch = 0;
		PacketsSent = 0;
		PacketsLost = 0;
		PacketLoss = 0;
		PacketLossVariance = 0;
		PacketThrottle = EnetConstants.PeerDefaultPacketThrottle;
		PacketThrottleLimit = EnetConstants.PeerDefaultPacketThrottle;
		PacketThrottleCounter = 0;
		PacketThrottleEpoch = 0;
		PacketThrottleAcceleration = EnetConstants.PeerPacketThrottleAcceleration;
		PacketThrottleDeceleration = EnetConstants.PeerPacketThrottleDeceleration;
		PacketThrottleInterval = EnetConstants.PeerPacketThrottleInterval;
		PingInterval = EnetConstants.PeerPingInterval;
		TimeoutLimit = EnetConstants.PeerTimeoutLimit;
		TimeoutMinimum = EnetConstants.PeerTimeoutMinimum;
		TimeoutMaximum = EnetConstants.PeerTimeoutMaximum;
		LastRoundTripTime = EnetConstants.PeerDefaultRoundTripTime;
		LowestRoundTripTime = EnetConstants.PeerDefaultRoundTripTime;
		LastRoundTripTimeVariance = 0;
		HighestRoundTripTimeVariance = 0;
		RoundTripTime = EnetConstants.PeerDefaultRoundTripTime;
		RoundTripTimeVariance = 0;
		Mtu = (uint)Host.Mtu;
		WindowSize = EnetConstants.ProtocolMaximumWindowSize;
		ReliableDataInTransit = 0;
		OutgoingReliableSequenceNumber = 0;
		OutgoingPeerId = EnetConstants.ProtocolMaximumPeerId;
		ConnectId = 0;
		State = EnetPeerState.Disconnected;
		IncomingUnsequencedGroup = 0;
		OutgoingUnsequencedGroup = 0;
		EventData = 0;
		PendingDisconnectReason = null;
		TotalWaitingData = 0;
		Array.Clear(UnsequencedWindow);
		ResetQueues();
	}

	internal void ResetQueues()
	{
		if (NeedsDispatch)
		{
			Host.RemoveDispatch(this);
			NeedsDispatch = false;
		}

		Acknowledgements.Clear();
		ReleaseOutgoingList(SentReliableCommands);
		ReleaseOutgoingList(SentUnreliableCommands);
		ReleaseOutgoingList(OutgoingReliableCommands);
		ReleaseOutgoingList(OutgoingUnreliableCommands);

		while (DispatchedCommands.First is { } node)
		{
			DispatchedCommands.RemoveFirst();
			ReleaseIncoming(node.Value);
		}

		foreach (var channel in Channels)
		{
			while (channel.IncomingReliableCommands.First is { } r)
			{
				channel.IncomingReliableCommands.RemoveFirst();
				ReleaseIncoming(r.Value);
			}
			while (channel.IncomingUnreliableCommands.First is { } u)
			{
				channel.IncomingUnreliableCommands.RemoveFirst();
				ReleaseIncoming(u.Value);
			}
		}

		Channels = Array.Empty<EnetChannel>();
	}

	private static void ReleaseOutgoingList(LinkedList<OutgoingCommand> list)
	{
		while (list.First is { } node)
		{
			list.RemoveFirst();
			ReleasePacketReference(node.Value.Packet);
		}
	}

	internal static void ReleasePacketReference(EnetPacket? packet)
	{
		if (packet is null) return;
		packet.ReferenceCount--;
		if (packet.ReferenceCount <= 0 && (packet.Flags & EnetPacketFlags.Sent) == 0)
			packet.Dispose();
	}

	private void ReleaseIncoming(IncomingCommand command)
	{
		if (command.Packet is { } packet)
		{
			int length = packet.Length;
			packet.ReferenceCount--;
			if (packet.ReferenceCount <= 0)
				packet.Dispose();
			TotalWaitingData -= length;
		}
	}

	public int Send(byte channelId, EnetPacket packet)
	{
		ArgumentNullException.ThrowIfNull(packet);
		if (State != EnetPeerState.Connected || channelId >= Channels.Length)
			return -1;
		if (packet.Length > Host.MaximumPacketSize)
			return -1;

		return Host.QueuePacket(this, channelId, packet);
	}

	public EnetPacket? Receive(out byte channelId)
	{
		channelId = 0;
		if (DispatchedCommands.First is not { } node)
			return null;

		DispatchedCommands.RemoveFirst();
		IncomingCommand command = node.Value;
		channelId = command.Command.ChannelId;
		EnetPacket? packet = command.Packet;
		if (packet is null) return null;

		packet.ReferenceCount--;
		TotalWaitingData -= packet.Length;
		command.Packet = null;
		return packet;
	}

	public void Ping()
	{
		if (State != EnetPeerState.Connected) return;
		Host.QueueOutgoingCommand(this, new ProtocolCommandData
		{
			Command = (byte)((byte)EnetProtocolCommand.Ping | EnetConstants.ProtocolCommandFlagAcknowledge),
			ChannelId = 0xFF,
		}, null, 0, 0);
	}

	public void SetPingInterval(uint interval) => PingInterval = interval == 0 ? EnetConstants.PeerPingInterval : interval;

	public void SetTimeout(uint limit, uint minimum, uint maximum)
	{
		TimeoutLimit = limit == 0 ? EnetConstants.PeerTimeoutLimit : limit;
		TimeoutMinimum = minimum == 0 ? EnetConstants.PeerTimeoutMinimum : minimum;
		TimeoutMaximum = maximum == 0 ? EnetConstants.PeerTimeoutMaximum : maximum;
	}

	public void ConfigureThrottle(uint interval, uint acceleration, uint deceleration)
	{
		PacketThrottleInterval = interval;
		PacketThrottleAcceleration = acceleration;
		PacketThrottleDeceleration = deceleration;

		Host.QueueOutgoingCommand(this, new ProtocolCommandData
		{
			Command = (byte)((byte)EnetProtocolCommand.ThrottleConfigure | EnetConstants.ProtocolCommandFlagAcknowledge),
			ChannelId = 0xFF,
			PacketThrottleInterval = interval,
			PacketThrottleAcceleration = acceleration,
			PacketThrottleDeceleration = deceleration,
		}, null, 0, 0);
	}

	internal int Throttle(uint roundTripTime)
	{
		if (LastRoundTripTime <= LastRoundTripTimeVariance)
		{
			PacketThrottle = PacketThrottleLimit;
			return 0;
		}

		if (roundTripTime < LastRoundTripTime)
		{
			PacketThrottle = Math.Min(PacketThrottle + PacketThrottleAcceleration, PacketThrottleLimit);
			return 1;
		}

		if (roundTripTime > LastRoundTripTime + 2 * LastRoundTripTimeVariance)
		{
			PacketThrottle = PacketThrottle > PacketThrottleDeceleration
				? PacketThrottle - PacketThrottleDeceleration
				: 0;
			return -1;
		}

		return 0;
	}

	public void DisconnectNow(uint data = 0) => Host.DisconnectNow(this, data);
	public void Disconnect(uint data = 0) => Host.Disconnect(this, data);
	public void DisconnectLater(uint data = 0) => Host.DisconnectLater(this, data);
	public void ResetPeer() => Reset();

	public override string ToString() => $"Peer#{IncomingPeerId} {Address} [{State}]";
}
