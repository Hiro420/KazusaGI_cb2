namespace KazusaGI_cb2.Enet;

public readonly record struct EnetEvent(
	EnetEventType Type,
	EnetPeer? Peer,
	byte ChannelId,
	uint Data,
	EnetPacket? Packet,
	string? Reason = null)
{
	public static readonly EnetEvent None = new(EnetEventType.None, null, 0, 0, null);
}
