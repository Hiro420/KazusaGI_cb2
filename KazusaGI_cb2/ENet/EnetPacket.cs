namespace KazusaGI_cb2.Enet;

public sealed class EnetPacket : IDisposable
{
	private bool _disposed;
	private byte[] _data;

	public EnetPacket(ReadOnlySpan<byte> data, EnetPacketFlags flags = EnetPacketFlags.None)
	{
		_data = data.ToArray();
		Flags = flags;
	}

	public EnetPacket(byte[] data, EnetPacketFlags flags = EnetPacketFlags.None, bool takeOwnership = false)
	{
		_data = takeOwnership || (flags & EnetPacketFlags.NoAllocate) != 0 ? data : data.ToArray();
		Flags = flags;
	}

	public byte[] Data { get => _data; internal set => _data = value; }
	public EnetPacketFlags Flags { get; internal set; }
	public object? UserData { get; set; }
	public Action<EnetPacket>? FreeCallback { get; set; }
	internal int ReferenceCount { get; set; }

	public int Length => Data.Length;

	public void Resize(int length)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
		Array.Resize(ref _data, length);
	}

	public void Dispose()
	{
		if (_disposed) return;
		_disposed = true;
		FreeCallback?.Invoke(this);
		_data = Array.Empty<byte>();
		GC.SuppressFinalize(this);
	}
}
