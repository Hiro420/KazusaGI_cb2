using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace KazusaGI_cb2.Enet;

public sealed partial class EnetHost
{
	public int CheckEvents(out EnetEvent enetEvent)
	{
		ThrowIfDisposed();
		return DispatchIncomingCommands(out enetEvent) ? 1 : 0;
	}

	public int Service(int timeoutMilliseconds, out EnetEvent enetEvent)
	{
		ThrowIfDisposed();
		if (timeoutMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds));

		if (DispatchIncomingCommands(out enetEvent))
			return 1;

		ServiceTime = ENet.TimeGet();
		uint timeout = unchecked(ServiceTime + (uint)timeoutMilliseconds);

		do
		{
			if (ENet.TimeDifference(ServiceTime, _bandwidthThrottleEpoch) >= EnetConstants.BandwidthThrottleInterval)
				BandwidthThrottle();

			if (SendOutgoingCommands(null, checkTimeouts: true) is { } sendEvent)
			{
				enetEvent = sendEvent;
				return 1;
			}

			if (ReceiveIncomingCommands(out EnetEvent receivedEvent))
			{
				enetEvent = receivedEvent;
				return 1;
			}

			if (SendOutgoingCommands(null, checkTimeouts: true) is { } secondSendEvent)
			{
				enetEvent = secondSendEvent;
				return 1;
			}

			if (DispatchIncomingCommands(out enetEvent))
				return 1;

			if (timeoutMilliseconds == 0)
				break;

			ServiceTime = ENet.TimeGet();
			if (ENet.TimeGreaterEqual(ServiceTime, timeout))
				break;

			uint remaining = ENet.TimeDifference(timeout, ServiceTime);
			int wait = checked((int)Math.Min(remaining, (uint)(int.MaxValue / 1000)));
			if (!_socket.Poll(wait * 1000, SelectMode.SelectRead))
			{
				ServiceTime = ENet.TimeGet();
				if (ENet.TimeGreaterEqual(ServiceTime, timeout))
					break;
			}
			else
			{
				ServiceTime = ENet.TimeGet();
			}
		}
		while (true);

		enetEvent = EnetEvent.None;
		return 0;
	}

	private bool DispatchIncomingCommands(out EnetEvent enetEvent)
	{
		while (_dispatchQueue.First is { } node)
		{
			EnetPeer peer = node.Value;
			_dispatchQueue.RemoveFirst();
			peer.NeedsDispatch = false;

			switch (peer.State)
			{
				case EnetPeerState.ConnectionPending:
				case EnetPeerState.ConnectionSucceeded:
					OnPeerConnect(peer);
					peer.State = EnetPeerState.Connected;
					enetEvent = new EnetEvent(EnetEventType.Connect, peer, 0, peer.EventData, null);
					return true;

				case EnetPeerState.Zombie:
					_recalculateBandwidthLimits = true;
					uint data = peer.EventData;
					string? reason = peer.PendingDisconnectReason;
					enetEvent = new EnetEvent(EnetEventType.Disconnect, peer, 0, data, null, reason);
					peer.Reset();
					return true;

				case EnetPeerState.Connected:
					EnetPacket? packet = peer.Receive(out byte channelId);
					if (packet is null) continue;
					if (peer.DispatchedCommands.Count != 0)
						QueueDispatch(peer);
					enetEvent = new EnetEvent(EnetEventType.Receive, peer, channelId, 0, packet);
					return true;
			}
		}

		enetEvent = EnetEvent.None;
		return false;
	}

	private bool ReceiveIncomingCommands(out EnetEvent enetEvent)
	{
		enetEvent = EnetEvent.None;
		for (int packetIndex = 0; packetIndex < 256; packetIndex++)
		{
			EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
			int received;
			try
			{
				received = _socket.ReceiveFrom(_receiveBuffer, 0, _receiveBuffer.Length, SocketFlags.None, ref remote);
			}
			catch (SocketException ex) when (ex.SocketErrorCode is SocketError.WouldBlock or SocketError.TryAgain)
			{
				return false;
			}
			catch (SocketException)
			{
				return false;
			}

			if (received <= 0) return false;
			TotalReceivedData += (ulong)received;
			TotalReceivedPackets++;

			var address = EnetAddress.FromEndPoint(remote);
			EnetEvent? immediate = HandleDatagram(address, _receiveBuffer.AsSpan(0, received));
			if (immediate is { } evt && evt.Type != EnetEventType.None)
			{
				enetEvent = evt;
				return true;
			}
		}
		return false;
	}

	private EnetEvent? HandleDatagram(EnetAddress address, ReadOnlySpan<byte> datagram)
	{
		if (datagram.Length < 2) return null;

		ushort peerField = BinaryPrimitives.ReadUInt16BigEndian(datagram);
		ushort peerId = (ushort)(peerField & EnetConstants.ProtocolHeaderPeerIdMask);
		ushort headerFlags = (ushort)(peerField & EnetConstants.ProtocolHeaderFlagMask);
		byte sessionId = (byte)((peerField & EnetConstants.ProtocolHeaderSessionMask) >> EnetConstants.ProtocolHeaderSessionShift);

		int headerLength = (headerFlags & EnetConstants.ProtocolHeaderFlagSentTime) != 0 ? 4 : 2;
		if (datagram.Length < headerLength) return null;
		ushort sentTime = headerLength == 4 ? BinaryPrimitives.ReadUInt16BigEndian(datagram.Slice(2, 2)) : (ushort)0;

		EnetPeer? peer = null;
		if (peerId != EnetConstants.ProtocolMaximumPeerId)
		{
			if (peerId >= _peers.Length) return null;
			peer = _peers[peerId];
			if (peer.State is EnetPeerState.Disconnected or EnetPeerState.Zombie) return null;
			if (!peer.Address.Equals(address)) return null;
			if (peer.OutgoingPeerId < EnetConstants.ProtocolMaximumPeerId && peer.IncomingSessionId != sessionId)
				return null;
		}

		// This build's checksum is unusual: the 32-bit checksum slot sits after
		// the ENet header, but the checksum itself is calculated over the
		// *uncompressed* command stream.  The COMPRESSED flag is already present
		// in the header while calculating it.  This ordering is visible in the
		// supplied binary and in the CB1 client's first CONNECT datagram.
		int checksumOffset = headerLength;
		uint expectedChecksum = 0;
		if (Checksum is not null)
		{
			if (datagram.Length < headerLength + 4) return null;
			expectedChecksum = BinaryPrimitives.ReadUInt32LittleEndian(datagram.Slice(checksumOffset, 4));
			headerLength += 4;
		}

		ReadOnlySpan<byte> payload = datagram.Slice(headerLength);
		if ((headerFlags & EnetConstants.ProtocolHeaderFlagCompressed) != 0)
		{
			if (_rangeCoder is null) return null;
			int decompressed = _rangeCoder.Decompress(payload, _packetBuffer);
			if (decompressed <= 0) return null;
			payload = _packetBuffer.AsSpan(0, decompressed);
		}

		if (Checksum is not null)
		{
			int baseHeaderLength = checksumOffset;
			byte[] checkBuffer = new byte[baseHeaderLength + 4 + payload.Length];
			datagram.Slice(0, baseHeaderLength).CopyTo(checkBuffer);
			uint seed = peer?.ConnectId ?? 0;
			BinaryPrimitives.WriteUInt32LittleEndian(checkBuffer.AsSpan(baseHeaderLength, 4), seed);
			payload.CopyTo(checkBuffer.AsSpan(baseHeaderLength + 4));
			uint actualChecksum = Checksum(checkBuffer);
			if (actualChecksum != expectedChecksum) return null;
		}

		if (peer is not null)
		{
			peer.Address = address;
			peer.IncomingDataTotal += checked((uint)datagram.Length);
		}

		int offset = 0;
		while (offset < payload.Length)
		{
			if (!ProtocolCodec.TryRead(payload.Slice(offset), out ProtocolCommandData command, out int commandSize))
				return null;

			if (peer is null && command.Kind != EnetProtocolCommand.Connect)
				return null;

			int dataLength = command.Kind switch
			{
				EnetProtocolCommand.SendReliable or EnetProtocolCommand.SendUnreliable or EnetProtocolCommand.SendUnsequenced
					or EnetProtocolCommand.SendFragment or EnetProtocolCommand.SendUnreliableFragment => command.DataLength,
				_ => 0,
			};
			if (offset + commandSize + dataLength > payload.Length)
				return null;

			ReadOnlySpan<byte> commandPayload = payload.Slice(offset + commandSize, dataLength);
			EnetEvent? immediate = HandleCommand(ref peer, address, sentTime, command, commandPayload);

			if (peer is not null && command.RequiresAcknowledgement)
			{
				switch (peer.State)
				{
					case EnetPeerState.Disconnected:
					case EnetPeerState.AcknowledgingConnect:
					case EnetPeerState.Disconnecting:
					case EnetPeerState.Zombie:
						break;
					case EnetPeerState.AcknowledgingDisconnect when command.Kind != EnetProtocolCommand.Disconnect:
						break;
					default:
						QueueAcknowledgement(peer, command, sentTime);
						break;
				}
			}

			if (immediate is { } evt && evt.Type != EnetEventType.None)
				return evt;

			offset += commandSize + dataLength;
		}

		return null;
	}

	private EnetEvent? HandleCommand(
		ref EnetPeer? peer,
		EnetAddress address,
		ushort sentTime,
		ProtocolCommandData command,
		ReadOnlySpan<byte> data)
	{
		switch (command.Kind)
		{
			case EnetProtocolCommand.Acknowledge:
				return peer is null ? null : HandleAcknowledge(peer, command);
			case EnetProtocolCommand.Connect:
				if (peer is not null) return null;
				peer = HandleConnect(address, command);
				return null;
			case EnetProtocolCommand.VerifyConnect:
				return peer is null ? null : HandleVerifyConnect(peer, command);
			case EnetProtocolCommand.Disconnect:
				return peer is null ? null : HandleDisconnect(peer, command);
			case EnetProtocolCommand.Ping:
				return null;
			case EnetProtocolCommand.SendReliable:
				if (peer is null || !CanReceiveData(peer, command.ChannelId)) return null;
				QueueIncomingCommand(peer, command, data, 0);
				return null;
			case EnetProtocolCommand.SendUnreliable:
				if (peer is null || !CanReceiveData(peer, command.ChannelId)) return null;
				QueueIncomingCommand(peer, command, data, 0);
				return null;
			case EnetProtocolCommand.SendFragment:
			case EnetProtocolCommand.SendUnreliableFragment:
				if (peer is null || !CanReceiveData(peer, command.ChannelId)) return null;
				HandleFragment(peer, command, data);
				return null;
			case EnetProtocolCommand.SendUnsequenced:
				if (peer is null || !CanReceiveData(peer, command.ChannelId)) return null;
				HandleUnsequenced(peer, command, data);
				return null;
			case EnetProtocolCommand.BandwidthLimit:
				if (peer is null || peer.State is not (EnetPeerState.Connected or EnetPeerState.DisconnectLater)) return null;
				HandleBandwidthLimit(peer, command);
				return null;
			case EnetProtocolCommand.ThrottleConfigure:
				if (peer is null || peer.State is not (EnetPeerState.Connected or EnetPeerState.DisconnectLater)) return null;
				peer.PacketThrottleInterval = command.PacketThrottleInterval;
				peer.PacketThrottleAcceleration = command.PacketThrottleAcceleration;
				peer.PacketThrottleDeceleration = command.PacketThrottleDeceleration;
				return null;
			default:
				return null;
		}
	}

	private static bool CanReceiveData(EnetPeer peer, byte channelId)
		=> channelId < peer.Channels.Length && (peer.State is EnetPeerState.Connected or EnetPeerState.DisconnectLater);

	private EnetPeer? HandleConnect(EnetAddress address, ProtocolCommandData command)
	{
		if (command.ChannelCount is < 1 or > EnetConstants.ProtocolMaximumChannelCount)
			return null;

		EnetPeer? selected = null;
		int duplicateCount = 0;
		foreach (EnetPeer candidate in _peers)
		{
			if (candidate.State != EnetPeerState.Disconnected)
			{
				if (candidate.State != EnetPeerState.Connecting && candidate.Address.Equals(address))
				{
					if (candidate.ConnectId == command.ConnectId)
						return null;
					duplicateCount++;
				}
			}
			else if (selected is null)
			{
				selected = candidate;
			}
		}

		if (selected is null || duplicateCount >= DuplicatePeers)
			return null;

		int channelCount = (int)Math.Min(command.ChannelCount, (uint)ChannelLimit);
		selected.Channels = CreateChannels(channelCount);
		selected.State = EnetPeerState.AcknowledgingConnect;
		selected.ConnectId = command.ConnectId;
		selected.Address = address;
		selected.OutgoingPeerId = command.OutgoingPeerId;
		selected.IncomingBandwidth = command.IncomingBandwidth;
		selected.OutgoingBandwidth = command.OutgoingBandwidth;
		selected.PacketThrottleInterval = command.PacketThrottleInterval;
		selected.PacketThrottleAcceleration = command.PacketThrottleAcceleration;
		selected.PacketThrottleDeceleration = command.PacketThrottleDeceleration;
		selected.EventData = command.Data;

		byte incoming = command.IncomingSessionId == 0xFF ? selected.OutgoingSessionId : command.IncomingSessionId;
		incoming = (byte)((incoming + 1) & 3);
		if (incoming == selected.OutgoingSessionId)
			incoming = (byte)((selected.OutgoingSessionId + 1) & 3);
		selected.OutgoingSessionId = incoming;

		byte outgoing = command.OutgoingSessionId == 0xFF ? selected.IncomingSessionId : command.OutgoingSessionId;
		outgoing = (byte)((outgoing + 1) & 3);
		if (outgoing == selected.IncomingSessionId)
			outgoing = (byte)((selected.IncomingSessionId + 1) & 3);
		selected.IncomingSessionId = outgoing;

		// Stable enet.dll starts a peer at host.mtu (1392 in this build),
		// clamps the remote CONNECT MTU to protocol bounds, then only lowers
		// the peer MTU if the remote value is smaller. It never raises the
		// live peer above the host default.
		uint remoteMtu = Math.Clamp(command.Mtu,
			EnetConstants.ProtocolMinimumMtu,
			EnetConstants.ProtocolMaximumMtu);
		selected.Mtu = Math.Min((uint)Mtu, remoteMtu);

		uint hostOutgoing = OutgoingBandwidth;
		uint remoteIncoming = selected.IncomingBandwidth;
		uint windowBandwidth;
		if (hostOutgoing == 0 && remoteIncoming == 0)
		{
			selected.WindowSize = EnetConstants.ProtocolMaximumWindowSize;
		}
		else
		{
			windowBandwidth = hostOutgoing == 0 ? remoteIncoming
				: remoteIncoming == 0 ? hostOutgoing
				: Math.Min(hostOutgoing, remoteIncoming);
			uint window = (windowBandwidth / EnetConstants.PeerWindowSizeScale)
						  * EnetConstants.ProtocolMinimumWindowSize;
			selected.WindowSize = Math.Clamp(window,
				EnetConstants.ProtocolMinimumWindowSize,
				EnetConstants.ProtocolMaximumWindowSize);
		}

		// The stable DLL caps the remote sender's advertised window from the
		// host *incoming* bandwidth (host+0x10), not outgoing bandwidth.
		uint incomingWindow = IncomingBandwidth == 0
			? EnetConstants.ProtocolMaximumWindowSize
			: (IncomingBandwidth / EnetConstants.PeerWindowSizeScale) * EnetConstants.ProtocolMinimumWindowSize;
		incomingWindow = Math.Clamp(incomingWindow,
			EnetConstants.ProtocolMinimumWindowSize,
			EnetConstants.ProtocolMaximumWindowSize);
		uint verifyWindow = Math.Clamp(command.WindowSize,
			EnetConstants.ProtocolMinimumWindowSize,
			EnetConstants.ProtocolMaximumWindowSize);
		verifyWindow = Math.Min(verifyWindow, incomingWindow);

		QueueOutgoingCommand(selected, new ProtocolCommandData
		{
			Command = (byte)((byte)EnetProtocolCommand.VerifyConnect | EnetConstants.ProtocolCommandFlagAcknowledge),
			ChannelId = 0xFF,
			OutgoingPeerId = selected.IncomingPeerId,
			IncomingSessionId = selected.OutgoingSessionId,
			OutgoingSessionId = selected.IncomingSessionId,
			Mtu = selected.Mtu,
			WindowSize = verifyWindow,
			ChannelCount = (uint)channelCount,
			IncomingBandwidth = IncomingBandwidth,
			OutgoingBandwidth = OutgoingBandwidth,
			PacketThrottleInterval = selected.PacketThrottleInterval,
			PacketThrottleAcceleration = selected.PacketThrottleAcceleration,
			PacketThrottleDeceleration = selected.PacketThrottleDeceleration,
			ConnectId = selected.ConnectId,
		}, null, 0, 0);

		return selected;
	}

	private EnetEvent? HandleVerifyConnect(EnetPeer peer, ProtocolCommandData command)
	{
		if (peer.State != EnetPeerState.Connecting)
			return null;

		bool invalid = command.ChannelCount is < 1 or > EnetConstants.ProtocolMaximumChannelCount
					   || command.PacketThrottleInterval != peer.PacketThrottleInterval
					   || command.PacketThrottleAcceleration != peer.PacketThrottleAcceleration
					   || command.PacketThrottleDeceleration != peer.PacketThrottleDeceleration
					   || command.ConnectId != peer.ConnectId;

		if (invalid)
		{
			// Custom behavior visible in the supplied binary: verify failure enters
			// ZOMBIE and dispatches a disconnect rather than silently resetting.
			peer.EventData = 0;
			OnPeerDisconnect(peer);
			peer.State = EnetPeerState.Zombie;
			QueueDispatch(peer);
			return null;
		}

		RemoveConnectCommand(peer);

		if (command.ChannelCount < peer.Channels.Length)
			Array.Resize(ref peer.Channels, checked((int)command.ChannelCount));

		peer.OutgoingPeerId = command.OutgoingPeerId;
		peer.IncomingSessionId = command.IncomingSessionId;
		peer.OutgoingSessionId = command.OutgoingSessionId;
		peer.Mtu = Math.Min(peer.Mtu, Math.Clamp(command.Mtu,
			EnetConstants.ProtocolMinimumMtu,
			EnetConstants.ProtocolMaximumMtu));
		peer.WindowSize = Math.Min(peer.WindowSize, Math.Clamp(command.WindowSize,
			EnetConstants.ProtocolMinimumWindowSize,
			EnetConstants.ProtocolMaximumWindowSize));
		peer.IncomingBandwidth = command.IncomingBandwidth;
		peer.OutgoingBandwidth = command.OutgoingBandwidth;
		_recalculateBandwidthLimits = true;

		OnPeerConnect(peer);
		peer.State = EnetPeerState.Connected;
		return new EnetEvent(EnetEventType.Connect, peer, 0, peer.EventData, null);
	}

	private void RemoveConnectCommand(EnetPeer peer)
	{
		LinkedListNode<OutgoingCommand>? node = peer.SentReliableCommands.First;
		while (node is not null)
		{
			var next = node.Next;
			if (node.Value.ReliableSequenceNumber == 1 && node.Value.Command.ChannelId == 0xFF)
			{
				RemoveOutgoingNode(peer, peer.SentReliableCommands, node, wasSentReliable: true);
				return;
			}
			node = next;
		}

		// CONNECT has no packet, so the stable 1.3.18 DLL queues it in the
		// general outgoingCommands list. Search both lists because an ACK can
		// race a retransmit that has already been requeued.
		if (RemoveQueuedReliableCommand(peer, peer.OutgoingUnreliableCommands, 1, 0xFF))
			return;
		_ = RemoveQueuedReliableCommand(peer, peer.OutgoingReliableCommands, 1, 0xFF);
	}

	private EnetEvent? HandleAcknowledge(EnetPeer peer, ProtocolCommandData command)
	{
		if (peer.State is EnetPeerState.Disconnected or EnetPeerState.Zombie)
			return null;

		uint serviceTime = ServiceTime;
		uint sentTime = (serviceTime & 0xFFFF0000u) | command.ReceivedSentTime;
		if ((command.ReceivedSentTime & 0x8000) > (serviceTime & 0x8000))
			sentTime = unchecked(sentTime - 0x10000);
		uint roundTripTime = ENet.TimeDifference(serviceTime, sentTime);
		if (roundTripTime >= EnetConstants.TimeOverflow)
			return null;
		if (roundTripTime == 0)
			roundTripTime = 1;

		bool hadPreviousReceive = peer.LastReceiveTime != 0;
		if (hadPreviousReceive)
			peer.Throttle(roundTripTime);
		UpdateRoundTripTime(peer, roundTripTime, hadPreviousReceive);

		// 1.3.18 stores a non-zero sentinel even if serviceTime is still 0/1.
		peer.LastReceiveTime = serviceTime <= 1 ? 1u : serviceTime;
		peer.EarliestTimeout = 0;

		EnetProtocolCommand removed = RemoveSentReliableCommand(peer,
			command.ReceivedReliableSequenceNumber,
			command.ChannelId);
		if (removed == EnetProtocolCommand.None)
			return null;

		if (peer.State == EnetPeerState.DisconnectLater
			&& peer.OutgoingReliableCommands.Count == 0
			&& peer.OutgoingUnreliableCommands.Count == 0
			&& peer.SentReliableCommands.Count == 0)
		{
			Disconnect(peer, peer.EventData);
			return null;
		}

		if (peer.State == EnetPeerState.Disconnecting)
		{
			if (removed != EnetProtocolCommand.Disconnect)
				return null;
			return NotifyDisconnect(peer, immediate: true);
		}

		if (peer.State == EnetPeerState.AcknowledgingConnect)
		{
			if (removed != EnetProtocolCommand.VerifyConnect)
				return null;
			_recalculateBandwidthLimits = true;
			OnPeerConnect(peer);
			peer.State = EnetPeerState.Connected;
			return new EnetEvent(EnetEventType.Connect, peer, 0, peer.EventData, null);
		}

		return null;
	}

	private static void UpdateRoundTripTime(EnetPeer peer, uint sample, bool hadPreviousReceive)
	{
		uint rtt;
		uint variance;

		if (!hadPreviousReceive)
		{
			// Stable 1.3.18 DLL initializes its estimator from the first ACK,
			// instead of smoothing from the reset-time 500 ms placeholder.
			rtt = sample;
			variance = (sample + 1) / 2;
		}
		else
		{
			rtt = peer.RoundTripTime;
			variance = peer.RoundTripTimeVariance - peer.RoundTripTimeVariance / 4;

			if (sample < rtt)
			{
				uint difference = rtt - sample;
				variance += difference / 4;
				rtt -= difference / 8;
			}
			else
			{
				uint difference = sample - rtt;
				variance += difference / 4;
				rtt += difference / 8;
			}
		}

		peer.RoundTripTime = rtt;
		peer.RoundTripTimeVariance = variance;
		if (peer.LowestRoundTripTime > rtt) peer.LowestRoundTripTime = rtt;
		if (peer.HighestRoundTripTimeVariance < variance) peer.HighestRoundTripTimeVariance = variance;

		uint serviceTime = ServiceTimeStatic(peer);
		if (peer.PacketThrottleEpoch == 0
			|| ENet.TimeDifference(serviceTime, peer.PacketThrottleEpoch) >= peer.PacketThrottleInterval)
		{
			peer.LastRoundTripTime = peer.LowestRoundTripTime;
			peer.LastRoundTripTimeVariance = Math.Max(peer.HighestRoundTripTimeVariance, 1u);
			peer.LowestRoundTripTime = rtt;
			peer.HighestRoundTripTimeVariance = variance;
			peer.PacketThrottleEpoch = serviceTime;
		}
	}

	private static uint ServiceTimeStatic(EnetPeer peer) => peer.Host.ServiceTime;

	private EnetProtocolCommand RemoveSentReliableCommand(EnetPeer peer, ushort sequence, byte channelId)
	{
		LinkedListNode<OutgoingCommand>? node = peer.SentReliableCommands.First;
		while (node is not null)
		{
			var next = node.Next;
			OutgoingCommand outgoing = node.Value;
			if (outgoing.ReliableSequenceNumber == sequence && outgoing.Command.ChannelId == channelId)
			{
				EnetProtocolCommand kind = outgoing.Command.Kind;
				RemoveOutgoingNode(peer, peer.SentReliableCommands, node, wasSentReliable: true);
				UpdateNextTimeout(peer);
				return kind;
			}
			node = next;
		}

		// Stable sub_18000A5E0 searches sentReliableCommands first, then
		// outgoingCommands (+0x110), then outgoingSendReliableCommands
		// (+0x100). This catches ACKs that race a timeout/requeue.
		EnetProtocolCommand queued = RemoveQueuedReliableCommandKind(
			peer, peer.OutgoingUnreliableCommands, sequence, channelId);
		if (queued != EnetProtocolCommand.None)
			return queued;

		return RemoveQueuedReliableCommandKind(
			peer, peer.OutgoingReliableCommands, sequence, channelId);
	}

	private bool RemoveQueuedReliableCommand(
		EnetPeer peer,
		LinkedList<OutgoingCommand> list,
		ushort sequence,
		byte channelId)
		=> RemoveQueuedReliableCommandKind(peer, list, sequence, channelId) != EnetProtocolCommand.None;

	private EnetProtocolCommand RemoveQueuedReliableCommandKind(
		EnetPeer peer,
		LinkedList<OutgoingCommand> list,
		ushort sequence,
		byte channelId)
	{
		LinkedListNode<OutgoingCommand>? node = FindQueuedReliableForAck(list, sequence, channelId);
		if (node is null)
			return EnetProtocolCommand.None;

		EnetProtocolCommand kind = node.Value.Command.Kind;
		RemoveOutgoingNode(peer, list, node, wasSentReliable: false);
		return kind;
	}

	internal static LinkedListNode<OutgoingCommand>? FindQueuedReliableForAck(
		LinkedList<OutgoingCommand> list,
		ushort sequence,
		byte channelId)
	{
		LinkedListNode<OutgoingCommand>? node = list.First;
		while (node is not null)
		{
			OutgoingCommand outgoing = node.Value;

			// Exact sub_18000A540 behavior: non-ACK commands are skipped, but
			// the first ACK-required command that has never been sent is a hard
			// search barrier. A late ACK may match a timed-out/requeued command
			// before that barrier; it must never jump across newly queued ACK
			// commands to remove a later entry.
			if ((outgoing.Command.Command & EnetConstants.ProtocolCommandFlagAcknowledge) != 0)
			{
				if (outgoing.SendAttempts == 0)
					return null;

				if (outgoing.ReliableSequenceNumber == sequence
					&& outgoing.Command.ChannelId == channelId)
					return node;
			}

			node = node.Next;
		}

		return null;
	}

	private void RemoveOutgoingNode(EnetPeer peer, LinkedList<OutgoingCommand> list, LinkedListNode<OutgoingCommand> node, bool wasSentReliable)
	{
		OutgoingCommand outgoing = node.Value;
		list.Remove(node);
		if (outgoing.SendAttempts != 0)
			ReleaseReliableWindow(peer, outgoing);
		if (wasSentReliable && outgoing.Packet is not null)
			peer.ReliableDataInTransit = peer.ReliableDataInTransit >= outgoing.FragmentLength
				? peer.ReliableDataInTransit - outgoing.FragmentLength
				: 0;
		ReleaseSentPacket(outgoing.Packet);
	}

	private static void ReleaseSentPacket(EnetPacket? packet)
	{
		if (packet is null) return;
		packet.ReferenceCount--;
		if (packet.ReferenceCount <= 0)
		{
			packet.Flags |= EnetPacketFlags.Sent;
			packet.Dispose();
		}
	}

	private void UpdateNextTimeout(EnetPeer peer)
	{
		if (peer.SentReliableCommands.First is { } first)
			peer.NextTimeout = unchecked(first.Value.SentTime + first.Value.RoundTripTimeout);
		else
			peer.NextTimeout = 0;
	}

	private EnetEvent? HandleDisconnect(EnetPeer peer, ProtocolCommandData command)
	{
		if (peer.State is EnetPeerState.Disconnected or EnetPeerState.Zombie or EnetPeerState.AcknowledgingDisconnect)
			return null;
		;
		peer.PendingDisconnectReason = "remote";
		peer.ResetQueues();
		EnetPeerState oldState = peer.State;

		if (oldState is EnetPeerState.ConnectionSucceeded or EnetPeerState.Disconnecting or EnetPeerState.Connecting)
		{
			OnPeerDisconnect(peer);
			peer.State = EnetPeerState.Zombie;
			QueueDispatch(peer);
		}
		else if (oldState is not (EnetPeerState.Connected or EnetPeerState.DisconnectLater))
		{
			if (oldState == EnetPeerState.ConnectionPending)
				_recalculateBandwidthLimits = true;
			peer.Reset();
		}
		else if (command.RequiresAcknowledgement)
		{
			OnPeerDisconnect(peer);
			peer.State = EnetPeerState.AcknowledgingDisconnect;
		}
		else
		{
			OnPeerDisconnect(peer);
			peer.State = EnetPeerState.Zombie;
			QueueDispatch(peer);
		}

		peer.EventData = command.Data;
		return null;
	}

	private EnetEvent? NotifyDisconnect(EnetPeer peer, bool immediate)
	{
		if ((int)peer.State > 2)
			_recalculateBandwidthLimits = true;

		if (peer.State != EnetPeerState.Connecting && peer.State <= EnetPeerState.ConnectionPending)
		{
			peer.Reset();
			return null;
		}

		if (immediate)
		{
			var evt = new EnetEvent(EnetEventType.Disconnect, peer, 0, 0, null,
				peer.PendingDisconnectReason ?? "notify");
			peer.Reset();
			return evt;
		}

		peer.EventData = 0;
		OnPeerDisconnect(peer);
		peer.State = EnetPeerState.Zombie;
		QueueDispatch(peer);
		return null;
	}

	private void QueueAcknowledgement(EnetPeer peer, ProtocolCommandData command, ushort sentTime)
	{
		if (command.ChannelId < peer.Channels.Length)
		{
			EnetChannel channel = peer.Channels[command.ChannelId];
			ushort reliableWindow = (ushort)(command.ReliableSequenceNumber / EnetConstants.ProtocolReliableWindowSize);
			ushort currentWindow = (ushort)(channel.IncomingReliableSequenceNumber / EnetConstants.ProtocolReliableWindowSize);
			if (command.ReliableSequenceNumber < channel.IncomingReliableSequenceNumber)
				reliableWindow += EnetConstants.ProtocolReliableWindows;
			// Native enet_peer_queue_acknowledgement drops only windows
			// (current + 6, current + 8], i.e. +7 and +8. Window +6 is still
			// acknowledged. The previous >= check incorrectly suppressed it.
			if (reliableWindow > currentWindow + EnetConstants.ProtocolFreeReliableWindows - 2
				&& reliableWindow <= currentWindow + EnetConstants.ProtocolFreeReliableWindows)
				return;
		}

		peer.OutgoingDataTotal = unchecked(peer.OutgoingDataTotal + 8);
		peer.Acknowledgements.AddLast(new Acknowledgement
		{
			SentTime = sentTime,
			Command = command.Clone(),
		});
	}

	private void QueueIncomingCommand(EnetPeer peer, ProtocolCommandData command, ReadOnlySpan<byte> data, uint fragmentCount)
	{
		EnetChannel channel = peer.Channels[command.ChannelId];
		bool unreliable = command.Kind is EnetProtocolCommand.SendUnreliable or EnetProtocolCommand.SendUnsequenced
						  or EnetProtocolCommand.SendUnreliableFragment;

		if (peer.State == EnetPeerState.DisconnectLater)
			return;
		if (peer.TotalWaitingData >= MaximumWaitingData)
			return;

		ushort reliableSequence = command.ReliableSequenceNumber;
		ushort unreliableSequence = command.Kind switch
		{
			EnetProtocolCommand.SendUnreliable => command.UnreliableSequenceNumber,
			EnetProtocolCommand.SendUnreliableFragment => command.StartSequenceNumber,
			_ => 0,
		};

		if (!unreliable)
		{
			// Stable 1.3.18 rejects SEND_RELIABLE/SEND_FRAGMENT when the
			// sequence is exactly the already-delivered incoming reliable
			// sequence. This is the normal duplicate-retransmit case after an
			// ACK is lost. It still gets ACKed by the protocol layer, but must
			// never be inserted into the reliable list or it can block current+1.
			if (!IsReliableDataSequenceAcceptable(
					channel.IncomingReliableSequenceNumber, reliableSequence))
				return;
			if (!IsReliableSequenceAcceptable(channel, reliableSequence)) return;
			if (channel.IncomingReliableCommands.Any(c => c.ReliableSequenceNumber == reliableSequence)) return;
		}
		else if (command.Kind != EnetProtocolCommand.SendUnsequenced)
		{
			if (!IsReliableSequenceAcceptable(channel, reliableSequence)) return;
			if (reliableSequence == channel.IncomingReliableSequenceNumber
				&& unreliableSequence <= channel.IncomingUnreliableSequenceNumber)
				return;
			if (channel.IncomingUnreliableCommands.Any(c => c.ReliableSequenceNumber == reliableSequence
															&& c.UnreliableSequenceNumber == unreliableSequence))
				return;
		}

		EnetPacketFlags packetFlags = command.Kind switch
		{
			EnetProtocolCommand.SendReliable or EnetProtocolCommand.SendFragment => EnetPacketFlags.Reliable,
			EnetProtocolCommand.SendUnsequenced => EnetPacketFlags.Unsequenced,
			EnetProtocolCommand.SendUnreliableFragment => EnetPacketFlags.UnreliableFragment,
			_ => EnetPacketFlags.None,
		};
		var packet = new EnetPacket(data, packetFlags);
		packet.ReferenceCount = 1;
		var incoming = new IncomingCommand
		{
			ReliableSequenceNumber = reliableSequence,
			UnreliableSequenceNumber = unreliableSequence,
			Command = command.Clone(),
			FragmentCount = fragmentCount,
			FragmentsRemaining = fragmentCount,
			Packet = packet,
			Fragments = fragmentCount == 0 ? null : new uint[checked((int)((fragmentCount + 31) / 32))],
		};
		peer.TotalWaitingData += packet.Length;

		if (!unreliable)
		{
			InsertIncomingSorted(channel.IncomingReliableCommands, incoming, reliableOnly: true);
			DispatchIncomingReliable(peer, channel);
		}
		else if (command.Kind == EnetProtocolCommand.SendUnsequenced)
		{
			peer.DispatchedCommands.AddLast(incoming);
			QueueDispatch(peer);
		}
		else
		{
			InsertIncomingSorted(channel.IncomingUnreliableCommands, incoming, reliableOnly: false);
			DispatchIncomingUnreliable(peer, channel);
		}
	}

	internal static bool IsReliableDataSequenceAcceptable(ushort incomingReliableSequence, ushort sequence)
		=> sequence != incomingReliableSequence;

	private static bool IsReliableSequenceAcceptable(EnetChannel channel, ushort sequence)
	{
		int incomingWindow = channel.IncomingReliableSequenceNumber / EnetConstants.ProtocolReliableWindowSize;
		int reliableWindow = sequence / EnetConstants.ProtocolReliableWindowSize;
		if (sequence < channel.IncomingReliableSequenceNumber)
			reliableWindow += EnetConstants.ProtocolReliableWindows;
		// Native enet_peer_queue_incoming_command accepts through current+6.
		// current+7 is outside the receive window even though the ACK filter
		// separately suppresses +7/+8.
		return reliableWindow >= incomingWindow
			   && reliableWindow <= incomingWindow + EnetConstants.ProtocolFreeReliableWindows - 2;
	}

	private static void InsertIncomingSorted(LinkedList<IncomingCommand> list, IncomingCommand incoming, bool reliableOnly)
	{
		var node = list.First;
		while (node is not null)
		{
			IncomingCommand current = node.Value;
			int compare = CompareSequence(incoming.ReliableSequenceNumber, current.ReliableSequenceNumber);
			if (compare < 0 || (!reliableOnly && compare == 0
				&& CompareSequence(incoming.UnreliableSequenceNumber, current.UnreliableSequenceNumber) < 0))
			{
				list.AddBefore(node, incoming);
				return;
			}
			node = node.Next;
		}
		list.AddLast(incoming);
	}

	private static int CompareSequence(ushort a, ushort b) => unchecked((short)(a - b));

	private void DispatchIncomingReliable(EnetPeer peer, EnetChannel channel)
	{
		while (channel.IncomingReliableCommands.First is { } node)
		{
			IncomingCommand command = node.Value;
			if (command.FragmentsRemaining != 0) break;
			if (command.ReliableSequenceNumber != unchecked((ushort)(channel.IncomingReliableSequenceNumber + 1))) break;

			channel.IncomingReliableCommands.RemoveFirst();
			channel.IncomingReliableSequenceNumber = command.ReliableSequenceNumber;
			if (command.FragmentCount != 0)
				channel.IncomingReliableSequenceNumber = unchecked((ushort)(channel.IncomingReliableSequenceNumber + command.FragmentCount - 1));
			channel.IncomingUnreliableSequenceNumber = 0;
			peer.DispatchedCommands.AddLast(command);
			QueueDispatch(peer);
		}

		DispatchIncomingUnreliable(peer, channel);
	}

	private void DispatchIncomingUnreliable(EnetPeer peer, EnetChannel channel)
	{
		var node = channel.IncomingUnreliableCommands.First;
		while (node is not null)
		{
			var next = node.Next;
			IncomingCommand command = node.Value;

			if (command.ReliableSequenceNumber == channel.IncomingReliableSequenceNumber
				&& command.FragmentsRemaining == 0
				&& (command.Command.Kind == EnetProtocolCommand.SendUnsequenced
					|| command.UnreliableSequenceNumber > channel.IncomingUnreliableSequenceNumber))
			{
				if (command.Command.Kind != EnetProtocolCommand.SendUnsequenced)
					channel.IncomingUnreliableSequenceNumber = command.UnreliableSequenceNumber;
				channel.IncomingUnreliableCommands.Remove(node);
				peer.DispatchedCommands.AddLast(command);
				QueueDispatch(peer);
			}
			else if (CompareSequence(command.ReliableSequenceNumber, channel.IncomingReliableSequenceNumber) < 0)
			{
				channel.IncomingUnreliableCommands.Remove(node);
				if (command.Packet is { } packet)
				{
					peer.TotalWaitingData -= packet.Length;
					packet.Dispose();
				}
			}

			node = next;
		}
	}

	private void HandleFragment(EnetPeer peer, ProtocolCommandData command, ReadOnlySpan<byte> fragmentData)
	{
		if (command.FragmentCount == 0 || command.FragmentCount > EnetConstants.ProtocolMaximumFragmentCount) return;
		if (command.FragmentNumber >= command.FragmentCount) return;
		if (command.TotalLength > MaximumPacketSize) return;
		if (command.FragmentOffset >= command.TotalLength) return;
		if ((ulong)command.FragmentOffset + command.DataLength > command.TotalLength) return;

		EnetChannel channel = peer.Channels[command.ChannelId];
		bool unreliable = command.Kind == EnetProtocolCommand.SendUnreliableFragment;
		LinkedList<IncomingCommand> list = unreliable ? channel.IncomingUnreliableCommands : channel.IncomingReliableCommands;
		ushort reliableSequence = unreliable ? command.ReliableSequenceNumber : command.StartSequenceNumber;
		ushort unreliableSequence = unreliable ? command.StartSequenceNumber : (ushort)0;

		// The native fragment handlers perform the same receive-window and
		// already-delivered duplicate checks as enet_peer_queue_incoming_command
		// before looking up/creating the reassembly entry. Skipping these checks
		// lets a retransmitted old fragment create a stale list head.
		if (!IsReliableSequenceAcceptable(channel, reliableSequence))
			return;
		if (!unreliable)
		{
			if (!IsReliableDataSequenceAcceptable(
					channel.IncomingReliableSequenceNumber, reliableSequence))
				return;
		}
		else if (reliableSequence == channel.IncomingReliableSequenceNumber
				 && unreliableSequence <= channel.IncomingUnreliableSequenceNumber)
		{
			return;
		}

		IncomingCommand? incoming = list.FirstOrDefault(c =>
			c.ReliableSequenceNumber == reliableSequence
			&& (!unreliable || c.UnreliableSequenceNumber == unreliableSequence));

		if (incoming is null)
		{
			var template = command.Clone();
			template.ReliableSequenceNumber = reliableSequence;
			template.UnreliableSequenceNumber = unreliableSequence;
			EnetPacketFlags flags = unreliable ? EnetPacketFlags.UnreliableFragment : EnetPacketFlags.Reliable;
			var packet = new EnetPacket(new byte[checked((int)command.TotalLength)], flags, takeOwnership: true)
			{
				ReferenceCount = 1,
			};
			incoming = new IncomingCommand
			{
				ReliableSequenceNumber = reliableSequence,
				UnreliableSequenceNumber = unreliableSequence,
				Command = template,
				FragmentCount = command.FragmentCount,
				FragmentsRemaining = command.FragmentCount,
				Packet = packet,
				Fragments = new uint[checked((int)((command.FragmentCount + 31) / 32))],
			};
			peer.TotalWaitingData += packet.Length;
			if (peer.TotalWaitingData > MaximumWaitingData)
			{
				peer.TotalWaitingData -= packet.Length;
				packet.Dispose();
				return;
			}
			InsertIncomingSorted(list, incoming, reliableOnly: !unreliable);
		}
		else if (incoming.FragmentCount != command.FragmentCount || incoming.Packet?.Length != command.TotalLength)
		{
			return;
		}

		int word = checked((int)(command.FragmentNumber >> 5));
		uint bit = 1u << (int)(command.FragmentNumber & 31);
		if ((incoming.Fragments![word] & bit) != 0) return;

		incoming.Fragments[word] |= bit;
		incoming.FragmentsRemaining--;
		fragmentData.CopyTo(incoming.Packet!.Data.AsSpan(checked((int)command.FragmentOffset), command.DataLength));

		if (incoming.FragmentsRemaining == 0)
		{
			if (unreliable) DispatchIncomingUnreliable(peer, channel);
			else DispatchIncomingReliable(peer, channel);
		}
	}

	private void HandleUnsequenced(EnetPeer peer, ProtocolCommandData command, ReadOnlySpan<byte> data)
	{
		// Exact 1.3.18 windowing from the stable KazusaGI DLL
		// (sub_18000B1D0). The accepted group range is a half-ring (0x8000),
		// while the duplicate bitmap itself covers the current 1024-group block.
		uint group = command.UnsequencedGroup;
		uint index = group % 1024u;
		uint incomingBase = peer.IncomingUnsequencedGroup;

		if (group < incomingBase)
			group += 0x10000u;
		if (group >= incomingBase + 0x8000u)
			return;

		ushort blockBase = unchecked((ushort)(group - index));
		int word = checked((int)(index >> 5));
		uint bit = 1u << checked((int)(index & 31));

		if (blockBase == peer.IncomingUnsequencedGroup)
		{
			if ((peer.UnsequencedWindow[word] & bit) != 0)
				return;
		}
		else
		{
			peer.IncomingUnsequencedGroup = blockBase;
			Array.Clear(peer.UnsequencedWindow);
		}

		// Native queue_incoming_command returns a non-null sentinel in
		// DisconnectLater (so the unsequenced bitmap is still consumed), but
		// returns null on the waiting-data limit. Preserve that distinction.
		if (peer.State != EnetPeerState.DisconnectLater)
		{
			if (peer.TotalWaitingData >= MaximumWaitingData)
				return;
			QueueIncomingCommand(peer, command, data, 0);
		}

		peer.UnsequencedWindow[word] |= bit;
	}

	private void HandleBandwidthLimit(EnetPeer peer, ProtocolCommandData command)
	{
		if (peer.IncomingBandwidth != 0 && command.IncomingBandwidth == 0)
			BandwidthLimitedPeers--;
		else if (peer.IncomingBandwidth == 0 && command.IncomingBandwidth != 0)
			BandwidthLimitedPeers++;
		peer.IncomingBandwidth = command.IncomingBandwidth;
		peer.OutgoingBandwidth = command.OutgoingBandwidth;
		_recalculateBandwidthLimits = true;

		uint incoming = peer.IncomingBandwidth;
		uint outgoing = OutgoingBandwidth;
		if (incoming == 0 && outgoing == 0)
		{
			peer.WindowSize = EnetConstants.ProtocolMaximumWindowSize;
			return;
		}

		uint bandwidth = outgoing == 0 ? incoming : incoming == 0 ? outgoing : Math.Min(incoming, outgoing);
		uint window = (bandwidth / EnetConstants.PeerWindowSizeScale) * EnetConstants.ProtocolMinimumWindowSize;
		peer.WindowSize = Math.Clamp(window,
			EnetConstants.ProtocolMinimumWindowSize,
			EnetConstants.ProtocolMaximumWindowSize);
	}
}
