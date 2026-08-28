namespace KazusaGI_cb2.GameServer;

/// <summary>
/// Near-direct C# reconstruction of hk4e CB2 gameserver/src/misc/guid_mgr.cpp.
/// Layout: [unix time:32][sequence low 12 bits:12][server id:8][0x1:type].
/// The sequence is process-global just like GuidMgr::genGuid(GuidType)::seq.
/// </summary>
public sealed class GuidMgr
{
	public enum GuidType : uint
	{
		None = 0,
		Avatar = 1,
		Item = 2,
		Mail = 3,
	}

	private static int _sequence;
	private readonly uint _serverId;

	public GuidMgr(uint serverId)
	{
		if (serverId > 0xFF)
			throw new ArgumentOutOfRangeException(nameof(serverId), "hk4e GUID server id is 8-bit");
		_serverId = serverId;
	}

	public static bool IsGuidOfType(ulong guid, GuidType type)
	{
		// Old KazusaGI_cb2 saves used tiny session-local counters. Real hk4e
		// GUIDs always carry a creation timestamp, the 0x10 marker, and the
		// object type in the low nibble.
		return guid != 0
			&& (guid >> 32) != 0
			&& (guid & 0x10UL) != 0
			&& (guid & 0xFUL) == ((ulong)type & 0xFUL);
	}

	public ulong GenGuid(GuidType type)
	{
		uint seq = unchecked((uint)Interlocked.Increment(ref _sequence));
		uint now = unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

		ulong low = ((ulong)(seq & 0xFFF) << 20)
					| ((ulong)(_serverId & 0xFF) << 12)
					| 0x10UL
					| ((ulong)type & 0xFUL);
		return ((ulong)now << 32) | low;
	}
}
