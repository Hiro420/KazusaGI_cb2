using System.Net;

namespace KazusaGI_cb2.Enet;

public readonly record struct EnetAddress(IPAddress Host, ushort Port)
{
	public static EnetAddress Any(ushort port) => new(IPAddress.Any, port);
	public static EnetAddress Loopback(ushort port) => new(IPAddress.Loopback, port);

	internal IPEndPoint ToEndPoint() => new(Host.MapToIPv4(), Port);

	internal static EnetAddress FromEndPoint(EndPoint endPoint)
	{
		var ip = (IPEndPoint)endPoint;
		return new EnetAddress(ip.Address.MapToIPv4(), checked((ushort)ip.Port));
	}

	public override string ToString() => $"{Host}:{Port}";
}
