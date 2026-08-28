using System.Net;

namespace KazusaGI_cb2.Enet;

/// <summary>Process-wide helpers corresponding to the small platform layer exported by ENet.</summary>
public static class ENet
{
	private static readonly object TimeLock = new();
	private static long _timeBase = Environment.TickCount64;
	private static uint _timeOffset;

	public static uint LinkedVersion => EnetConstants.LinkedVersion;

	public static int Initialize() => 0;
	public static void Deinitialize() { }

	public static uint TimeGet()
	{
		lock (TimeLock)
			return unchecked((uint)(Environment.TickCount64 - _timeBase) + _timeOffset);
	}

	public static void TimeSet(uint newTimeBase)
	{
		lock (TimeLock)
		{
			_timeBase = Environment.TickCount64;
			_timeOffset = newTimeBase;
		}
	}

	internal static bool TimeLess(uint a, uint b)
		=> unchecked(a - b) >= EnetConstants.TimeOverflow;

	internal static bool TimeGreaterEqual(uint a, uint b) => !TimeLess(a, b);

	internal static uint TimeDifference(uint a, uint b)
	{
		uint difference = unchecked(a - b);
		return difference >= EnetConstants.TimeOverflow ? unchecked(b - a) : difference;
	}

	internal static uint HostRandomSeed()
		=> unchecked((uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds());

	public static EnetPacket PacketCreate(ReadOnlySpan<byte> data, EnetPacketFlags flags = EnetPacketFlags.None)
		=> new(data, flags);

	public static EnetPacket PacketCreate(byte[] data, EnetPacketFlags flags = EnetPacketFlags.None, bool takeOwnership = false)
		=> new(data, flags, takeOwnership);

	public static void PacketDestroy(EnetPacket? packet) => packet?.Dispose();

	public static EnetAddress AddressFromHost(string host, ushort port)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(host);
		if (IPAddress.TryParse(host, out IPAddress? address))
			return new EnetAddress(address.MapToIPv4(), port);

		IPAddress? resolved = Dns.GetHostAddresses(host)
			.FirstOrDefault(static ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
		if (resolved is null)
			throw new InvalidOperationException($"Unable to resolve IPv4 host '{host}'.");
		return new EnetAddress(resolved, port);
	}
}
