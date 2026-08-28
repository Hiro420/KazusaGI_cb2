namespace KazusaGI_cb2.Enet;

/// <summary>
/// Managed reconstruction of the adaptive range coder in the attached libenet.so.7.
/// The native implementation stores the model as a 4096-node binary tree; this
/// implementation stores each context by byte value while preserving the exact
/// frequencies, rescale rules, order-2 context chain and arithmetic intervals.
/// Tree shape is only a lookup optimization and does not affect the bitstream.
/// </summary>
public sealed class EnetRangeCoder
{
	private const uint Top = 0x00FFFFFFu;
	private const uint Bottom = 0x0000FFFFu;
	private const int MaximumSymbols = 0xFFD; // native resets when nextSymbol > 0xFFD

	private sealed class Symbol
	{
		public Symbol(byte value, int count)
		{
			Value = value;
			Count = count;
		}

		public byte Value;
		public int Count;
		public int Escapes;
		public int Total;
		public Symbol? Parent;
		public SortedDictionary<byte, Symbol> Children { get; } = new();
	}

	private Symbol _root = null!;
	private Symbol _state = null!;
	private int _nextSymbol;
	private int _order;

	public EnetRangeCoder() => Reset();

	private void Reset()
	{
		_root = new Symbol(0, 0)
		{
			Escapes = 1,
			Total = 257,
			Parent = null,
		};
		_state = _root;
		_nextSymbol = 1;
		_order = 0;
	}

	private Symbol Create(byte value, int count)
	{
		_nextSymbol++;
		return new Symbol(value, count);
	}

	private static int SumCountsBefore(Symbol context, byte value)
	{
		int sum = 0;
		foreach (var pair in context.Children)
		{
			if (pair.Key >= value) break;
			sum += pair.Value.Count;
		}
		return sum;
	}

	private static void Rescale(Symbol context, bool root)
	{
		int total = 0;
		foreach (var symbol in context.Children.Values)
		{
			symbol.Count -= symbol.Count >> 1;
			total += symbol.Count;
		}

		context.Escapes -= context.Escapes >> 1;
		context.Total = context.Escapes + total + (root ? 256 : 0);
	}

	private static bool NormalizeEncode(ref uint low, ref uint range, Span<byte> output, ref int outputOffset)
	{
		while (true)
		{
			if ((low ^ unchecked(low + range)) > Top)
			{
				if (range > Bottom)
					return true;
				range = unchecked((ushort)(0 - (ushort)low));
			}

			if ((uint)outputOffset >= (uint)output.Length)
				return false;

			output[outputOffset++] = (byte)(low >> 24);
			low = unchecked(low << 8);
			range = unchecked(range << 8);
		}
	}

	private static void NormalizeDecode(ref uint code, ref uint low, ref uint range, ReadOnlySpan<byte> input, ref int inputOffset)
	{
		while (true)
		{
			if ((low ^ unchecked(low + range)) > Top)
			{
				if (range > Bottom)
					return;
				range = unchecked((ushort)(0 - (ushort)low));
			}

			code = unchecked(code << 8);
			if ((uint)inputOffset < (uint)input.Length)
				code |= input[inputOffset++];
			low = unchecked(low << 8);
			range = unchecked(range << 8);
		}
	}

	public int Compress(ReadOnlySpan<byte> input, Span<byte> output)
	{
		Reset();
		if (input.IsEmpty || output.IsEmpty)
			return 0;

		uint low = 0;
		uint range = 0xFFFFFFFFu;
		int outputOffset = 0;

		foreach (byte value in input)
		{
			Symbol context = _state;
			Symbol? firstForNextContext = null;
			Symbol? previousForLink = null;
			bool encoded = false;

			while (!ReferenceEquals(context, _root))
			{
				int oldEscapes = context.Escapes;
				int oldTotal = context.Total;

				if (context.Children.TryGetValue(value, out Symbol? symbol))
				{
					int oldCount = symbol.Count;
					if (oldCount != 0 && oldEscapes != 0 && oldEscapes < oldTotal)
					{
						uint unit = range / (uint)oldTotal;
						if (unit == 0) return 0;
						uint cumulative = (uint)(oldEscapes + SumCountsBefore(context, value));
						low = unchecked(low + unit * cumulative);
						range = unchecked(unit * (uint)oldCount);
						if (!NormalizeEncode(ref low, ref range, output, ref outputOffset))
							return 0;

						symbol.Count += 2;
						context.Total += 2;

						if (firstForNextContext is null) firstForNextContext = symbol;
						if (previousForLink is not null) previousForLink.Parent = symbol;
						previousForLink = symbol;

						if (context.Total > 0xFF00 || oldCount > 0xFB)
							Rescale(context, root: false);

						encoded = true;
						break;
					}
				}
				else
				{
					symbol = Create(value, 2);
					context.Children.Add(value, symbol);
				}

				// Missing (or zero-count) symbol: the binary creates/updates it first,
				// then emits the context escape when this context has a usable model.
				if (firstForNextContext is null) firstForNextContext = symbol;
				if (previousForLink is not null) previousForLink.Parent = symbol;
				previousForLink = symbol;

				if (oldEscapes != 0 && oldEscapes < oldTotal)
				{
					uint unit = range / (uint)oldTotal;
					if (unit == 0) return 0;
					range = unchecked(unit * (uint)oldEscapes);
					if (!NormalizeEncode(ref low, ref range, output, ref outputOffset))
						return 0;
				}

				context.Escapes += 5;
				context.Total += 7;
				if (context.Total > 0xFF00)
					Rescale(context, root: false);

				context = context.Parent ?? _root;
			}

			if (!encoded)
			{
				int oldEscapes = _root.Escapes;
				int oldTotal = _root.Total;
				int oldCount;
				int effectiveCount;
				Symbol rootSymbol;

				if (_root.Children.TryGetValue(value, out Symbol? existing))
				{
					rootSymbol = existing;
					oldCount = rootSymbol.Count;
					effectiveCount = oldCount + 1;
				}
				else
				{
					rootSymbol = Create(value, 3);
					_root.Children.Add(value, rootSymbol);
					oldCount = 0;
					effectiveCount = 1;
				}

				uint unit = range / (uint)oldTotal;
				if (unit == 0) return 0;
				uint cumulative = (uint)(oldEscapes + value + SumCountsBefore(_root, value));
				low = unchecked(low + unit * cumulative);
				range = unchecked(unit * (uint)effectiveCount);
				if (!NormalizeEncode(ref low, ref range, output, ref outputOffset))
					return 0;

				if (oldCount != 0)
					rootSymbol.Count += 3;
				_root.Total += 3;

				if (firstForNextContext is null) firstForNextContext = rootSymbol;
				if (previousForLink is not null) previousForLink.Parent = rootSymbol;
				previousForLink = rootSymbol;

				if (_root.Total > 0xFF00 || effectiveCount > 0xFA)
					Rescale(_root, root: true);
			}

			_state = firstForNextContext ?? _root;
			if (_order <= 1)
				_order++;
			else
				_state = _state.Parent ?? _root;

			if (_nextSymbol > MaximumSymbols)
				Reset();
		}

		if (low != 0)
		{
			while (true)
			{
				if ((uint)outputOffset >= (uint)output.Length)
					return 0;
				output[outputOffset++] = (byte)(low >> 24);
				low = unchecked(low << 8);
				if (low == 0) break;
			}
		}

		return outputOffset;
	}

	public byte[] Compress(ReadOnlySpan<byte> input)
	{
		if (input.IsEmpty) return Array.Empty<byte>();
		byte[] buffer = new byte[Math.Max(64, checked(input.Length * 2 + 64))];
		int length = Compress(input, buffer);
		if (length == 0) return Array.Empty<byte>();
		Array.Resize(ref buffer, length);
		return buffer;
	}

	public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
	{
		Reset();
		if (input.IsEmpty) return 0;

		int inputOffset = 0;
		uint code = 0;
		for (int i = 0; i < 4; i++)
		{
			code <<= 8;
			if (inputOffset < input.Length)
				code |= input[inputOffset++];
		}

		uint low = 0;
		uint range = 0xFFFFFFFFu;
		int outputOffset = 0;

		while (true)
		{
			Symbol startContext = _state;
			Symbol context = startContext;
			Symbol foundContext = _root;
			Symbol? foundSymbol = null;
			byte value = 0;
			bool haveValue = false;

			while (!ReferenceEquals(context, _root))
			{
				int escapes = context.Escapes;
				int total = context.Total;

				if (escapes != 0 && escapes < total)
				{
					uint unit = range / (uint)total;
					if (unit == 0) return 0;
					uint scaled = unchecked(code - low) / unit;

					if (scaled < (uint)escapes)
					{
						range = unchecked(unit * (uint)escapes);
						NormalizeDecode(ref code, ref low, ref range, input, ref inputOffset);
						context = context.Parent ?? _root;
						continue;
					}

					uint target = scaled - (uint)escapes;
					int cumulative = 0;
					foreach (var pair in context.Children)
					{
						Symbol symbol = pair.Value;
						int next = cumulative + symbol.Count;
						if (target < (uint)next)
						{
							int oldCount = symbol.Count;
							low = unchecked(low + unit * (uint)(escapes + cumulative));
							range = unchecked(unit * (uint)oldCount);
							NormalizeDecode(ref code, ref low, ref range, input, ref inputOffset);

							symbol.Count += 2;
							context.Total += 2;
							if (context.Total > 0xFF00 || oldCount > 0xFB)
								Rescale(context, root: false);

							value = symbol.Value;
							foundContext = context;
							foundSymbol = symbol;
							haveValue = true;
							break;
						}
						cumulative = next;
					}

					if (haveValue) break;
					return 0; // malformed arithmetic interval
				}

				// Empty/uninitialized context: encoder emits no bits for the escape.
				context = context.Parent ?? _root;
			}

			if (!haveValue)
			{
				uint unit = range / (uint)_root.Total;
				if (unit == 0) return 0;
				uint scaled = unchecked(code - low) / unit;

				if (scaled < (uint)_root.Escapes)
				{
					// Root escape is the native stream terminator. Its final
					// normalization does not consume additional input bytes.
					range = unchecked(unit * (uint)_root.Escapes);
					while (true)
					{
						if ((low ^ unchecked(low + range)) > Top)
						{
							if (range > Bottom) break;
							range = unchecked((ushort)(0 - (ushort)low));
						}
						low = unchecked(low << 8);
						range = unchecked(range << 8);
					}
					return outputOffset;
				}

				uint target = scaled - (uint)_root.Escapes;
				int extraBefore = 0;
				bool decoded = false;
				for (int b = 0; b < 256; b++)
				{
					_root.Children.TryGetValue((byte)b, out Symbol? symbol);
					int count = 1 + (symbol?.Count ?? 0);
					uint begin = (uint)(b + extraBefore);
					uint end = begin + (uint)count;
					if (target < end)
					{
						value = (byte)b;
						int oldCount = symbol?.Count ?? 0;
						int effectiveCount = oldCount + 1;

						low = unchecked(low + unit * (uint)(_root.Escapes + begin));
						range = unchecked(unit * (uint)effectiveCount);
						NormalizeDecode(ref code, ref low, ref range, input, ref inputOffset);

						if (symbol is null)
						{
							symbol = Create(value, 3);
							_root.Children.Add(value, symbol);
						}
						else
						{
							symbol.Count += 3;
						}

						_root.Total += 3;
						if (_root.Total > 0xFF00 || effectiveCount > 0xFA)
							Rescale(_root, root: true);

						foundContext = _root;
						foundSymbol = symbol;
						decoded = true;
						break;
					}

					if (symbol is not null)
						extraBefore += symbol.Count;
				}

				if (!decoded || foundSymbol is null)
					return 0;
			}

			// Back-fill every higher-order context that escaped/skipped, exactly
			// like the native decoder's LABEL_46/LABEL_54 chain.
			Symbol? firstForNextContext = null;
			Symbol? previousForLink = null;
			context = startContext;
			while (!ReferenceEquals(context, foundContext))
			{
				int oldEscapes = context.Escapes;
				int oldTotal = context.Total;
				Symbol symbol;
				if (context.Children.TryGetValue(value, out Symbol? existing))
				{
					symbol = existing;
					int oldCount = symbol.Count;
					symbol.Count += 2;
					context.Total += 2;
					if (context.Total > 0xFF00 || oldCount > 0xFB)
						Rescale(context, root: false);
				}
				else
				{
					symbol = Create(value, 2);
					context.Children.Add(value, symbol);
					context.Escapes = oldEscapes + 5;
					context.Total = oldTotal + 7;
					if (context.Total > 0xFF00)
						Rescale(context, root: false);
				}

				if (firstForNextContext is null) firstForNextContext = symbol;
				if (previousForLink is not null) previousForLink.Parent = symbol;
				previousForLink = symbol;
				context = context.Parent ?? _root;
			}

			if (foundSymbol is null) return 0;
			if (firstForNextContext is null) firstForNextContext = foundSymbol;
			if (previousForLink is not null) previousForLink.Parent = foundSymbol;

			if ((uint)outputOffset >= (uint)output.Length)
				return 0;
			output[outputOffset++] = value;

			_state = firstForNextContext;
			if (_order <= 1)
				_order++;
			else
				_state = _state.Parent ?? _root;

			if (_nextSymbol > MaximumSymbols)
				Reset();
		}
	}

	public byte[] Decompress(ReadOnlySpan<byte> input, int maximumOutputLength)
	{
		if (maximumOutputLength < 0) throw new ArgumentOutOfRangeException(nameof(maximumOutputLength));
		byte[] output = new byte[maximumOutputLength];
		int length = Decompress(input, output);
		if (length == 0) return Array.Empty<byte>();
		Array.Resize(ref output, length);
		return output;
	}
}
