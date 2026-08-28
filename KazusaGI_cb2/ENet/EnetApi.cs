namespace KazusaGI_cb2.Enet;

/// <summary>
/// C-style convenience façade for callers porting code that used libenet directly.
/// The object-oriented EnetHost/EnetPeer API is the same implementation underneath.
/// </summary>
public static class EnetApi
{
	public static int enet_initialize() => ENet.Initialize();
	public static void enet_deinitialize() => ENet.Deinitialize();
	public static uint enet_linked_version() => ENet.LinkedVersion;
	public static uint enet_time_get() => ENet.TimeGet();
	public static void enet_time_set(uint value) => ENet.TimeSet(value);

	public static EnetHost? enet_host_create(
		EnetAddress? address,
		int peerCount,
		int channelLimit,
		uint incomingBandwidth,
		uint outgoingBandwidth)
	{
		try
		{
			return new EnetHost(address, peerCount, channelLimit, incomingBandwidth, outgoingBandwidth);
		}
		catch
		{
			return null;
		}
	}

	public static void enet_host_destroy(EnetHost? host) => host?.Dispose();

	public static EnetPeer? enet_host_connect(EnetHost host, EnetAddress address, int channelCount, uint data)
		=> host.Connect(address, channelCount, data);

	public static int enet_host_service(EnetHost host, out EnetEvent enetEvent, int timeout)
		=> host.Service(timeout, out enetEvent);

	public static int enet_host_check_events(EnetHost host, out EnetEvent enetEvent)
		=> host.CheckEvents(out enetEvent);

	public static void enet_host_flush(EnetHost host) => host.Flush();
	public static void enet_host_broadcast(EnetHost host, byte channelId, EnetPacket packet) => host.Broadcast(channelId, packet);
	public static void enet_host_channel_limit(EnetHost host, int channelLimit) => host.SetChannelLimit(channelLimit);
	public static void enet_host_bandwidth_limit(EnetHost host, uint incomingBandwidth, uint outgoingBandwidth)
		=> host.SetBandwidthLimit(incomingBandwidth, outgoingBandwidth);
	public static int enet_host_compress_with_range_coder(EnetHost host)
	{
		host.CompressWithRangeCoder();
		return 0;
	}
	public static void enet_host_checksum_with_crc32(EnetHost host) => host.EnableCrc32Checksum();

	public static EnetPacket enet_packet_create(ReadOnlySpan<byte> data, EnetPacketFlags flags)
		=> ENet.PacketCreate(data, flags);
	public static void enet_packet_destroy(EnetPacket? packet) => ENet.PacketDestroy(packet);
	public static int enet_packet_resize(EnetPacket packet, int dataLength)
	{
		try
		{
			packet.Resize(dataLength);
			return 0;
		}
		catch
		{
			return -1;
		}
	}

	public static int enet_peer_send(EnetPeer peer, byte channelId, EnetPacket packet) => peer.Send(channelId, packet);
	public static EnetPacket? enet_peer_receive(EnetPeer peer, out byte channelId) => peer.Receive(out channelId);
	public static void enet_peer_ping(EnetPeer peer) => peer.Ping();
	public static void enet_peer_ping_interval(EnetPeer peer, uint pingInterval) => peer.SetPingInterval(pingInterval);
	public static void enet_peer_timeout(EnetPeer peer, uint timeoutLimit, uint timeoutMinimum, uint timeoutMaximum)
		=> peer.SetTimeout(timeoutLimit, timeoutMinimum, timeoutMaximum);
	public static void enet_peer_throttle_configure(EnetPeer peer, uint interval, uint acceleration, uint deceleration)
		=> peer.ConfigureThrottle(interval, acceleration, deceleration);
	public static void enet_peer_disconnect(EnetPeer peer, uint data) => peer.Disconnect(data);
	public static void enet_peer_disconnect_now(EnetPeer peer, uint data) => peer.DisconnectNow(data);
	public static void enet_peer_disconnect_later(EnetPeer peer, uint data) => peer.DisconnectLater(data);
	public static void enet_peer_reset(EnetPeer peer) => peer.ResetPeer();

	public static uint enet_crc32(params ReadOnlyMemory<byte>[] buffers) => EnetCrc32.Compute(buffers);
}
