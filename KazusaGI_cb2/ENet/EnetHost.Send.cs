using System.Buffers.Binary;
using System.Net.Sockets;

namespace KazusaGI_cb2.Enet;

public sealed partial class EnetHost
{
	/// <summary>
	/// Reconstructed send side of enet_protocol_send_outgoing_commands.
	/// The stable Windows DLL keeps payload-reliable and general output queues,
	/// merges them by queue serial, reserves four bytes for the protocol header,
	/// and compresses
	/// only the command/payload body before the final header/checksum is emitted.
	/// </summary>
	private EnetEvent? SendOutgoingCommands(EnetEvent? _, bool checkTimeouts)
	{
		// The native routine loops again when one MTU-sized packet was unable to
		// drain all queued commands. Cap the pass count defensively so a hostile
		// caller cannot turn one Service() call into an unbounded loop.
		for (int pass = 0; pass < 256; pass++)
		{
			bool continueSending = false;

			foreach (EnetPeer peer in _peers)
			{
				if (peer.State is EnetPeerState.Disconnected or EnetPeerState.Zombie)
					continue;

				if (checkTimeouts
					&& peer.SentReliableCommands.Count != 0
					&& peer.NextTimeout != 0
					&& ENet.TimeGreaterEqual(ServiceTime, peer.NextTimeout))
				{
					EnetEvent? timeoutEvent = CheckTimeouts(peer);
					if (timeoutEvent is { } evt)
						return evt;
					if (peer.State is EnetPeerState.Disconnected or EnetPeerState.Zombie)
						continue;
				}

				bool peerHasMore;
				int sent = SendPeerDatagram(peer, out peerHasMore);
				if (sent < 0)
					continue;
				continueSending |= peerHasMore;
			}

			if (!continueSending)
				break;
		}

		return null;
	}

	private EnetEvent? CheckTimeouts(EnetPeer peer)
	{
		// sub_18000D960 snapshots the first node of both outgoing lists before
		// scanning sentReliableCommands. Timed-out commands are inserted before
		// those snapshot nodes, not repeatedly at the live list head. That
		// preserves the original sent order when several commands time out in
		// one pass (A,B -> A,B,oldHead rather than B,A,oldHead).
		LinkedListNode<OutgoingCommand>? sendReliableAnchor = peer.OutgoingReliableCommands.First;
		LinkedListNode<OutgoingCommand>? generalAnchor = peer.OutgoingUnreliableCommands.First;

		LinkedListNode<OutgoingCommand>? node = peer.SentReliableCommands.First;
		while (node is not null)
		{
			LinkedListNode<OutgoingCommand>? next = node.Next;
			OutgoingCommand outgoing = node.Value;
			uint elapsed = ENet.TimeDifference(ServiceTime, outgoing.SentTime);
			if (elapsed < outgoing.RoundTripTimeout)
			{
				node = next;
				continue;
			}

			if (peer.EarliestTimeout == 0 || ENet.TimeLess(outgoing.SentTime, peer.EarliestTimeout))
				peer.EarliestTimeout = outgoing.SentTime;

			uint earliestElapsed = ENet.TimeDifference(ServiceTime, peer.EarliestTimeout);
			// The stable 1.3.18 DLL no longer compares RTO against a cached
			// roundTripTimeoutLimit.  It gates the minimum timeout by the
			// exponential retry count: 1 << (sendAttempts - 1) >= timeoutLimit.
			uint attemptScale = outgoing.SendAttempts == 0
				? 0
				: outgoing.SendAttempts >= 32
					? uint.MaxValue
					: 1u << (outgoing.SendAttempts - 1);
			if (earliestElapsed >= peer.TimeoutMaximum
				|| (attemptScale >= peer.TimeoutLimit && earliestElapsed >= peer.TimeoutMinimum))
			{
				peer.PendingDisconnectReason =
					$"timeout:{outgoing.Command.Kind}:ch{outgoing.Command.ChannelId}:seq{outgoing.ReliableSequenceNumber}";
				//Console.WriteLine(
				//    $"[enet] TIMEOUT peerId={peer.IncomingPeerId} connectId=0x{peer.ConnectId:X8} " +
				//    $"seq={outgoing.ReliableSequenceNumber} ch={outgoing.Command.ChannelId} " +
				//    $"kind={outgoing.Command.Kind} earliest={peer.EarliestTimeout} now={ServiceTime} " +
				//    $"elapsed={earliestElapsed} timeoutMax={peer.TimeoutMaximum} rto={outgoing.RoundTripTimeout} " +
				//    $"attemptScale={attemptScale} timeoutLimit={peer.TimeoutLimit} timeoutMin={peer.TimeoutMinimum} " +
				//    $"attempts={outgoing.SendAttempts}");
				return NotifyDisconnect(peer, immediate: true);
			}

			if (outgoing.Packet is not null)
				peer.ReliableDataInTransit = peer.ReliableDataInTransit >= outgoing.FragmentLength
					? peer.ReliableDataInTransit - outgoing.FragmentLength
					: 0;

			peer.PacketsLost = unchecked(peer.PacketsLost + 1);
			// The Windows 1.3.18 DLL doubles the command RTO exactly on each retry.
			outgoing.RoundTripTimeout = unchecked(outgoing.RoundTripTimeout * 2u);

			peer.SentReliableCommands.Remove(node);
			// Payload-bearing reliable commands return to
			// outgoingSendReliableCommands (+0x100); reliable controls return
			// to outgoingCommands (+0x110). Insert before the queue head that
			// existed when the timeout scan began, exactly like the DLL.
			if (outgoing.Packet is not null)
				InsertTimedOutBeforeAnchor(peer.OutgoingReliableCommands, sendReliableAnchor, outgoing);
			else
				InsertTimedOutBeforeAnchor(peer.OutgoingUnreliableCommands, generalAnchor, outgoing);
			node = next;
		}

		UpdateNextTimeout(peer);
		return null;
	}

	internal static void InsertTimedOutBeforeAnchor(
		LinkedList<OutgoingCommand> list,
		LinkedListNode<OutgoingCommand>? anchor,
		OutgoingCommand command)
	{
		if (anchor is null)
			list.AddLast(command);
		else
			list.AddBefore(anchor, command);
	}

	private int SendPeerDatagram(EnetPeer peer, out bool hasMore)
	{
		hasMore = false;

		// Match the native idle-ping rule: if no new reliable data is waiting and
		// the peer has not received anything for PingInterval, enqueue a ping.
		if (peer.State == EnetPeerState.Connected
			&& peer.OutgoingReliableCommands.Count == 0
			&& peer.OutgoingUnreliableCommands.Count == 0
			&& peer.SentReliableCommands.Count == 0
			&& ENet.TimeDifference(ServiceTime, peer.LastReceiveTime) >= peer.PingInterval)
		{
			// Stable enet.dll only idles pings when there is no reliable command
			// already in flight.  Without this guard, repeated 500 ms PINGs can
			// accumulate behind one lost ACK and become the earliest timeout.
			peer.Ping();
		}

		int mtu = checked((int)peer.Mtu);
		int checksumLength = Checksum is null ? 0 : 4;
		int maximumBodyLength = mtu - 4 - checksumLength;
		if (maximumBodyLength <= 0)
			return 0;

		byte[] body = new byte[maximumBodyLength];
		int bodyLength = 0;
		int commandCount = 0;
		bool includeSentTime = false;
		bool packedAnything = false;

		// ACKs are emitted first by the native protocol routine.
		while (peer.Acknowledgements.First is { } ackNode)
		{
			if (commandCount >= EnetConstants.ProtocolMaximumPacketCommands || bodyLength + 8 > maximumBodyLength)
			{
				hasMore = true;
				break;
			}

			Acknowledgement ack = ackNode.Value;
			var command = new ProtocolCommandData
			{
				Command = (byte)EnetProtocolCommand.Acknowledge,
				ChannelId = ack.Command.ChannelId,
				ReliableSequenceNumber = ack.Command.ReliableSequenceNumber,
				ReceivedReliableSequenceNumber = ack.Command.ReliableSequenceNumber,
				ReceivedSentTime = ack.SentTime,
			};
			bodyLength += ProtocolCodec.Write(body.AsSpan(bodyLength), command);
			commandCount++;
			packedAnything = true;
			peer.Acknowledgements.RemoveFirst();

			// This is visible in the supplied binary: after ACKing an incoming
			// acknowledged Disconnect, the peer is moved to ZOMBIE/dispatch.
			if (ack.Command.Kind == EnetProtocolCommand.Disconnect)
			{
				OnPeerDisconnect(peer);
				peer.State = EnetPeerState.Zombie;
				QueueDispatch(peer);
			}
		}

		PackOutgoingCommands(peer, body, ref bodyLength, ref commandCount,
			maximumBodyLength, ref includeSentTime, ref packedAnything, ref hasMore);

		if (peer.State == EnetPeerState.DisconnectLater
			&& peer.OutgoingReliableCommands.Count == 0
			&& peer.OutgoingUnreliableCommands.Count == 0
			&& peer.SentReliableCommands.Count == 0)
		{
			uint data = peer.EventData;
			Disconnect(peer, data);
			hasMore = true;
		}

		if (!packedAnything)
			return 0;

		UpdatePacketLoss(peer);

		ReadOnlySpan<byte> finalBody = body.AsSpan(0, bodyLength);
		byte[]? compressed = null;
		bool isCompressed = false;
		if (_rangeCoder is not null && bodyLength > 0)
		{
			compressed = new byte[bodyLength];
			int compressedLength = _rangeCoder.Compress(finalBody, compressed);
			if (compressedLength > 0 && compressedLength < bodyLength)
			{
				finalBody = compressed.AsSpan(0, compressedLength);
				isCompressed = true;
			}
		}

		ushort headerFlags = 0;
		if (includeSentTime) headerFlags |= EnetConstants.ProtocolHeaderFlagSentTime;
		if (isCompressed) headerFlags |= EnetConstants.ProtocolHeaderFlagCompressed;

		int headerLength = includeSentTime ? 4 : 2;
		int datagramLength = headerLength + checksumLength + finalBody.Length;
		if (datagramLength > mtu)
		{
			// Packing uses the native worst-case four-byte header reservation, so
			// this should only be reachable with a custom checksum/header mismatch.
			hasMore = true;
			return 0;
		}

		byte[] datagram = new byte[datagramLength];
		ushort peerField;
		uint checksumSeed;
		if (peer.OutgoingPeerId >= EnetConstants.ProtocolMaximumPeerId)
		{
			peerField = (ushort)(EnetConstants.ProtocolMaximumPeerId | headerFlags);
			checksumSeed = 0;
		}
		else
		{
			peerField = (ushort)(peer.OutgoingPeerId
								 | ((peer.OutgoingSessionId & 3) << EnetConstants.ProtocolHeaderSessionShift)
								 | headerFlags);
			checksumSeed = peer.ConnectId;
		}

		BinaryPrimitives.WriteUInt16BigEndian(datagram.AsSpan(0, 2), peerField);
		int offset = 2;
		if (includeSentTime)
		{
			BinaryPrimitives.WriteUInt16BigEndian(datagram.AsSpan(offset, 2), unchecked((ushort)ServiceTime));
			offset += 2;
		}

		int checksumOffset = -1;
		uint checksumValue = 0;
		if (Checksum is not null)
		{
			checksumOffset = offset;

			// The supplied build calculates CRC32 before range-coding the body.
			// The wire header (including COMPRESSED) and connect-id seed are used,
			// followed by the original uncompressed commands.
			byte[] checksumInput = new byte[headerLength + 4 + bodyLength];
			datagram.AsSpan(0, headerLength).CopyTo(checksumInput);
			BinaryPrimitives.WriteUInt32LittleEndian(checksumInput.AsSpan(headerLength, 4), checksumSeed);
			body.AsSpan(0, bodyLength).CopyTo(checksumInput.AsSpan(headerLength + 4));
			checksumValue = Checksum(checksumInput);

			BinaryPrimitives.WriteUInt32LittleEndian(datagram.AsSpan(offset, 4), checksumValue);
			offset += 4;
		}

		finalBody.CopyTo(datagram.AsSpan(offset));

		// Native ENet records lastSendTime before calling sendto(), regardless
		// of whether the non-blocking socket ultimately reports an error.
		peer.LastSendTime = ServiceTime;

		int sent;
		try
		{
			sent = _socket.SendTo(datagram, SocketFlags.None, peer.Address.ToEndPoint());
		}
		catch (SocketException ex) when (ex.SocketErrorCode is SocketError.WouldBlock or SocketError.TryAgain)
		{
			sent = 0;
		}
		catch (SocketException)
		{
			sent = -1;
		}

		ReleaseSentUnreliableCommands(peer);

		if (sent >= 0)
		{
			TotalSentData += (ulong)sent;
			TotalSentPackets++;
		}

		// The packer marks hasMore only when another immediate pass can make
		// progress. Do not spin solely because a reliable queue is blocked on
		// ACK/window state; that state can change only after receiving traffic.
		hasMore |= peer.Acknowledgements.Count != 0;
		return sent;
	}

	private void PackOutgoingCommands(
		EnetPeer peer,
		byte[] body,
		ref int bodyLength,
		ref int commandCount,
		int maximumBodyLength,
		ref bool includeSentTime,
		ref bool packedAnything,
		ref bool hasMore)
	{
		// Stable enet.dll 1.3.18 has two outgoing lists:
		//   +0x100 outgoingSendReliableCommands: ACK-required commands WITH payload
		//   +0x110 outgoingCommands: everything else
		// sub_18000DC60 merges them by OutgoingCommand.queueTime. This is not
		// equivalent to draining a "reliable" list before an "unreliable" list.
		LinkedListNode<OutgoingCommand>? sendReliable = peer.OutgoingReliableCommands.First;
		LinkedListNode<OutgoingCommand>? general = peer.OutgoingUnreliableCommands.First;
		bool sendReliableBlocked = false;

		while (sendReliable is not null || general is not null)
		{
			bool fromSendReliable;
			LinkedListNode<OutgoingCommand> node;

			if (sendReliable is null)
			{
				node = general!;
				fromSendReliable = false;
			}
			else if (general is null)
			{
				node = sendReliable;
				fromSendReliable = true;
			}
			else if (GeneralQueueComesFirst(sendReliable.Value.QueueTime, general.Value.QueueTime))
			{
				node = general;
				fromSendReliable = false;
			}
			else
			{
				node = sendReliable;
				fromSendReliable = true;
			}

			LinkedListNode<OutgoingCommand>? next = node.Next;
			OutgoingCommand outgoing = node.Value;
			ProtocolCommandData command = outgoing.Command;
			bool acknowledged = (command.Command & EnetConstants.ProtocolCommandFlagAcknowledge) != 0;
			int commandSize = ProtocolCodec.GetCommandSize(command.Command);
			int payloadLength = outgoing.Packet is null ? 0 : outgoing.FragmentLength;

			EnetChannel? channel = command.ChannelId < peer.Channels.Length
				? peer.Channels[command.ChannelId]
				: null;

			if (acknowledged && channel is not null && outgoing.SendAttempts == 0
				&& (outgoing.ReliableSequenceNumber & (EnetConstants.ProtocolReliableWindowSize - 1)) == 0)
			{
				int reliableWindow = outgoing.ReliableSequenceNumber / EnetConstants.ProtocolReliableWindowSize;
				int previousWindow = (reliableWindow + EnetConstants.ProtocolReliableWindows - 1)
									 % EnetConstants.ProtocolReliableWindows;
				uint windowMask = ReliableWindowExclusionMask(reliableWindow);

				if (channel.ReliableWindows[previousWindow] >= EnetConstants.ProtocolReliableWindowSize
					|| (channel.UsedReliableWindows & windowMask) != 0)
				{
					// In the DLL this condition disables the +0x100
					// outgoingSendReliableCommands iterator but continues to
					// service the general queue. ACK control commands normally
					// use channel 0xFF, so they are not held behind a data window.
					if (fromSendReliable)
					{
						sendReliable = null;
						sendReliableBlocked = true;
						continue;
					}
				}
			}

			if (acknowledged && outgoing.Packet is not null)
			{
				uint reliableWindowSize = Math.Max(
					(peer.PacketThrottle * peer.WindowSize) / EnetConstants.PeerPacketThrottleScale,
					peer.Mtu);
				if (peer.ReliableDataInTransit + outgoing.FragmentLength > reliableWindowSize)
				{
					if (fromSendReliable)
					{
						sendReliable = null;
						sendReliableBlocked = true;
						continue;
					}
				}
			}

			if (commandCount >= EnetConstants.ProtocolMaximumPacketCommands
				|| bodyLength + commandSize + payloadLength > maximumBodyLength)
			{
				hasMore = true;
				break;
			}

			if (!acknowledged && outgoing.Packet is not null && outgoing.FragmentOffset == 0)
			{
				peer.PacketThrottleCounter = (peer.PacketThrottleCounter + EnetConstants.PeerPacketThrottleCounter)
											 % EnetConstants.PeerPacketThrottleScale;
				if (peer.PacketThrottleCounter > peer.PacketThrottle)
				{
					// Non-ACK packet commands only live in the general queue.
					ushort reliable = outgoing.ReliableSequenceNumber;
					ushort unreliable = outgoing.UnreliableSequenceNumber;
					DropUnreliableGroup(peer, node, reliable, unreliable, out general);
					continue;
				}
			}

			if (acknowledged)
			{
				if (channel is not null && outgoing.SendAttempts == 0)
				{
					int reliableWindow = outgoing.ReliableSequenceNumber / EnetConstants.ProtocolReliableWindowSize;
					channel.UsedReliableWindows |= 1u << reliableWindow;
					channel.ReliableWindows[reliableWindow]++;
				}

				outgoing.SendAttempts++;
				if (outgoing.RoundTripTimeout == 0)
				{
					outgoing.RoundTripTimeout = unchecked(peer.RoundTripTime + 4 * peer.RoundTripTimeVariance);
					if (outgoing.RoundTripTimeout == 0)
						outgoing.RoundTripTimeout = 1;
				}

				if (peer.SentReliableCommands.Count == 0)
					peer.NextTimeout = unchecked(ServiceTime + outgoing.RoundTripTimeout);

				if (fromSendReliable)
				{
					peer.OutgoingReliableCommands.Remove(node);
					sendReliable = next;
				}
				else
				{
					peer.OutgoingUnreliableCommands.Remove(node);
					general = next;
				}

				peer.SentReliableCommands.AddLast(outgoing);
				outgoing.SentTime = ServiceTime;
				includeSentTime = true;

				if (outgoing.Packet is not null)
					peer.ReliableDataInTransit = unchecked(peer.ReliableDataInTransit + outgoing.FragmentLength);
			}
			else
			{
				if (fromSendReliable)
					throw new InvalidOperationException("non-ACK command entered outgoingSendReliableCommands");

				peer.OutgoingUnreliableCommands.Remove(node);
				general = next;
				if (outgoing.Packet is not null)
					peer.SentUnreliableCommands.AddLast(outgoing);
			}

			bodyLength += ProtocolCodec.Write(body.AsSpan(bodyLength), command);
			if (outgoing.Packet is { } packet)
			{
				packet.Data.AsSpan(checked((int)outgoing.FragmentOffset), outgoing.FragmentLength)
					.CopyTo(body.AsSpan(bodyLength, outgoing.FragmentLength));
				bodyLength += outgoing.FragmentLength;
			}

			// enet.dll increments packetsSent for every command selected by
			// sub_18000DC60, not just ACK-required commands.
			peer.PacketsSent = unchecked(peer.PacketsSent + 1);
			packedAnything = true;
			commandCount++;
		}

		// A reliable-data queue blocked only by window/in-flight limits must
		// wait for ACK progress; asking SendOutgoingCommands to immediately
		// spin another pass cannot make it sendable. Capacity/order deferrals
		// above set hasMore explicitly when another immediate pass can help.
		if (general is not null)
			hasMore = true;
		if (sendReliable is not null && !sendReliableBlocked)
			hasMore = true;
	}

	internal static bool GeneralQueueComesFirst(uint sendReliableQueueTime, uint generalQueueTime)
		=> unchecked(sendReliableQueueTime - generalQueueTime) < EnetConstants.TimeOverflow;

	internal static uint ReliableWindowExclusionMask(int reliableWindow)
	{
		// Exact 1.3.18 DLL predicate from the send loop:
		//   ((0x03FF >> (16 - window)) | (0x03FF << window)) & usedWindows
		// The ten-bit rotating mask leaves six forward reliable windows, which
		// matches the receive side accepting current..current+6.
		reliableWindow &= EnetConstants.ProtocolReliableWindows - 1;
		uint baseMask = 0x03FFu;
		int rightShift = EnetConstants.ProtocolReliableWindows - reliableWindow;
		uint wrapped = baseMask >> rightShift;
		return ((baseMask << reliableWindow) | wrapped) & 0xFFFFu;
	}

	private static void DropUnreliableGroup(
		EnetPeer peer,
		LinkedListNode<OutgoingCommand> first,
		ushort reliableSequence,
		ushort unreliableSequence,
		out LinkedListNode<OutgoingCommand>? nextAfterGroup)
	{
		LinkedListNode<OutgoingCommand>? node = first;
		nextAfterGroup = null;
		while (node is not null)
		{
			LinkedListNode<OutgoingCommand>? next = node.Next;
			OutgoingCommand value = node.Value;
			if (node != first
				&& (value.ReliableSequenceNumber != reliableSequence
					|| value.UnreliableSequenceNumber != unreliableSequence))
			{
				nextAfterGroup = node;
				return;
			}

			peer.OutgoingUnreliableCommands.Remove(node);
			EnetPeer.ReleasePacketReference(value.Packet);
			node = next;
		}
	}

	private static void ReleaseSentUnreliableCommands(EnetPeer peer)
	{
		while (peer.SentUnreliableCommands.First is { } node)
		{
			peer.SentUnreliableCommands.RemoveFirst();
			ReleaseSentPacket(node.Value.Packet);
		}
	}

	private void UpdatePacketLoss(EnetPeer peer)
	{
		if (peer.PacketLossEpoch == 0)
		{
			peer.PacketLossEpoch = ServiceTime;
			return;
		}

		if (ENet.TimeDifference(ServiceTime, peer.PacketLossEpoch) < EnetConstants.PeerPacketLossInterval)
			return;
		if (peer.PacketsSent == 0)
			return;

		uint packetLoss = (uint)(((ulong)peer.PacketsLost << 16) / peer.PacketsSent);
		uint difference = packetLoss >= peer.PacketLoss
			? packetLoss - peer.PacketLoss
			: peer.PacketLoss - packetLoss;

		// Stable DLL E570 uses the pre-update absolute difference for variance:
		//   variance = (difference + 3 * oldVariance) / 4
		//   loss     = (sample + 7 * oldLoss) / 8
		// v4/v5-preaudit incorrectly computed variance after partially moving
		// PacketLoss toward the sample.
		peer.PacketLossVariance = (difference + 3 * peer.PacketLossVariance) / 4;
		peer.PacketLoss = (packetLoss + 7 * peer.PacketLoss) / 8;
		peer.PacketLossEpoch = ServiceTime;
		peer.PacketsSent = 0;
		peer.PacketsLost = 0;
	}

	private void BandwidthThrottle()
	{
		uint time = ENet.TimeGet();
		uint elapsed = ENet.TimeDifference(time, _bandwidthThrottleEpoch);
		if (elapsed < EnetConstants.BandwidthThrottleInterval)
			return;
		_bandwidthThrottleEpoch = time;

		EnetPeer[] active = _peers
			.Where(static p => p.State is EnetPeerState.Connected or EnetPeerState.DisconnectLater)
			.ToArray();
		if (active.Length == 0)
			return;

		uint throttle = EnetConstants.PeerPacketThrottleScale;
		if (OutgoingBandwidth != 0)
		{
			ulong dataTotal = 0;
			foreach (EnetPeer peer in active)
				dataTotal += peer.OutgoingDataTotal;

			ulong bandwidth = (ulong)OutgoingBandwidth * elapsed / EnetConstants.BandwidthThrottleInterval;
			if (dataTotal > bandwidth && dataTotal != 0)
				throttle = (uint)((ulong)EnetConstants.PeerPacketThrottleScale * bandwidth / dataTotal);
		}

		foreach (EnetPeer peer in active)
		{
			uint peerThrottle = throttle;
			if (peer.IncomingBandwidth != 0)
			{
				ulong peerBandwidth = (ulong)peer.IncomingBandwidth * elapsed
									  / EnetConstants.BandwidthThrottleInterval;
				ulong scaled = ((ulong)peer.OutgoingDataTotal * peerThrottle)
							   / EnetConstants.PeerPacketThrottleScale;
				if (scaled > peerBandwidth && peer.OutgoingDataTotal != 0)
				{
					peerThrottle = (uint)Math.Clamp(
						(ulong)EnetConstants.PeerPacketThrottleScale * peerBandwidth / peer.OutgoingDataTotal,
						1UL,
						(ulong)EnetConstants.PeerPacketThrottleScale);
				}
			}

			peer.PacketThrottleLimit = peerThrottle;
			if (peer.PacketThrottle > peerThrottle)
				peer.PacketThrottle = peerThrottle;
			peer.OutgoingDataTotal = 0;
			peer.IncomingDataTotal = 0;
		}

		if (!_recalculateBandwidthLimits)
			return;

		_recalculateBandwidthLimits = false;
		uint bandwidthLimit = IncomingBandwidth;
		if (bandwidthLimit != 0 && active.Length != 0)
			bandwidthLimit /= (uint)active.Length;

		foreach (EnetPeer peer in active)
		{
			uint peerLimit = peer.OutgoingBandwidth != 0 && (bandwidthLimit == 0 || peer.OutgoingBandwidth < bandwidthLimit)
				? peer.OutgoingBandwidth
				: bandwidthLimit;

			QueueOutgoingCommand(peer, new ProtocolCommandData
			{
				Command = (byte)((byte)EnetProtocolCommand.BandwidthLimit | EnetConstants.ProtocolCommandFlagAcknowledge),
				ChannelId = 0xFF,
				IncomingBandwidth = peerLimit,
				OutgoingBandwidth = OutgoingBandwidth,
			}, null, 0, 0);
		}
	}

	private void ReleaseReliableWindow(EnetPeer peer, OutgoingCommand outgoing)
	{
		byte channelId = outgoing.Command.ChannelId;
		if (channelId >= peer.Channels.Length)
			return;

		EnetChannel channel = peer.Channels[channelId];
		int reliableWindow = outgoing.ReliableSequenceNumber / EnetConstants.ProtocolReliableWindowSize;
		if (channel.ReliableWindows[reliableWindow] != 0)
			channel.ReliableWindows[reliableWindow]--;
		if (channel.ReliableWindows[reliableWindow] == 0)
			channel.UsedReliableWindows &= ~(1u << reliableWindow);
	}
}
