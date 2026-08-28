using System.Buffers.Binary;

namespace KazusaGI_cb2.Enet;

public static class EnetCrc32
{
	private static readonly uint[] Table = CreateTable();

	private static uint[] CreateTable()
	{
		var table = new uint[256];
		for (uint n = 0; n < 256; n++)
		{
			uint crc = Reflect(n, 8) << 24;
			for (int i = 0; i < 8; i++)
				crc = (crc << 1) ^ ((crc & 0x80000000u) != 0 ? 0x04C11DB7u : 0u);
			table[n] = Reflect(crc, 32);
		}
		return table;
	}

	private static uint Reflect(uint value, int bits)
	{
		uint result = 0;
		for (int i = 0; i < bits; i++)
		{
			if ((value & 1) != 0)
				result |= 1u << (bits - 1 - i);
			value >>= 1;
		}
		return result;
	}

	public static uint Compute(params ReadOnlyMemory<byte>[] buffers)
	{
		uint crc = 0xFFFFFFFFu;
		foreach (var memory in buffers)
			foreach (byte b in memory.Span)
				crc = Table[(byte)(b ^ crc)] ^ (crc >> 8);

		// The native enet_crc32 returns ENET_HOST_TO_NET_32(~crc).
		uint value = ~crc;
		return BinaryPrimitives.ReverseEndianness(value);
	}

	internal static uint ComputeHostValue(ReadOnlySpan<byte> data)
	{
		uint crc = 0xFFFFFFFFu;
		foreach (byte b in data)
			crc = Table[(byte)(b ^ crc)] ^ (crc >> 8);
		return BinaryPrimitives.ReverseEndianness(~crc);
	}
}
