using Newtonsoft.Json;
using System.Globalization;

namespace KazusaGI_cb2.Resource.Json.Ability;

public enum AbilityScalarValueKind
{
	Float = 1,
	Int = 2,
	Bool = 3,
	String = 5,
	UInt = 6,
}

/// <summary>
/// C# representation of the value held by hk4e's DynamicArgument/std::any.
/// The kind is part of the data; values are never normalized to float merely
/// because a current gameplay caller only understands floats.
/// </summary>
[JsonConverter(typeof(AbilityScalarValueConverter))]
public readonly struct AbilityScalarValue
{
	public AbilityScalarValueKind Kind { get; }
	private readonly float _floatValue;
	private readonly int _intValue;
	private readonly uint _uintValue;
	private readonly bool _boolValue;
	private readonly string? _stringValue;

	private AbilityScalarValue(AbilityScalarValueKind kind, float floatValue = 0, int intValue = 0,
		uint uintValue = 0, bool boolValue = false, string? stringValue = null)
	{
		Kind = kind;
		_floatValue = floatValue;
		_intValue = intValue;
		_uintValue = uintValue;
		_boolValue = boolValue;
		_stringValue = stringValue;
	}

	public static AbilityScalarValue FromFloat(float value) => new(AbilityScalarValueKind.Float, floatValue: value);
	public static AbilityScalarValue FromInt(int value) => new(AbilityScalarValueKind.Int, intValue: value);
	public static AbilityScalarValue FromUInt(uint value) => new(AbilityScalarValueKind.UInt, uintValue: value);
	public static AbilityScalarValue FromBool(bool value) => new(AbilityScalarValueKind.Bool, boolValue: value);
	public static AbilityScalarValue FromString(string value) => new(AbilityScalarValueKind.String, stringValue: value);

	public float FloatValue => Kind == AbilityScalarValueKind.Float
		? _floatValue : throw new InvalidOperationException($"Ability scalar is {Kind}, not Float.");
	public int IntValue => Kind == AbilityScalarValueKind.Int
		? _intValue : throw new InvalidOperationException($"Ability scalar is {Kind}, not Int.");
	public uint UIntValue => Kind == AbilityScalarValueKind.UInt
		? _uintValue : throw new InvalidOperationException($"Ability scalar is {Kind}, not UInt.");
	public bool BoolValue => Kind == AbilityScalarValueKind.Bool
		? _boolValue : throw new InvalidOperationException($"Ability scalar is {Kind}, not Bool.");
	public string StringValue => Kind == AbilityScalarValueKind.String
		? _stringValue ?? string.Empty : throw new InvalidOperationException($"Ability scalar is {Kind}, not String.");

	public object BoxedValue => Kind switch
	{
		AbilityScalarValueKind.Float => _floatValue,
		AbilityScalarValueKind.Int => _intValue,
		AbilityScalarValueKind.UInt => _uintValue,
		AbilityScalarValueKind.Bool => _boolValue,
		AbilityScalarValueKind.String => _stringValue ?? string.Empty,
		_ => throw new InvalidOperationException($"Unsupported ability scalar kind {Kind}.")
	};

	/// <summary>
	/// Temporary compatibility bridge for the old float-only runtime manager.
	/// It does not mutate or erase the original scalar kind.
	/// </summary>
	public bool TryGetSingle(out float value)
	{
		switch (Kind)
		{
			case AbilityScalarValueKind.Float:
				value = _floatValue;
				return true;
			case AbilityScalarValueKind.Int:
				value = _intValue;
				return true;
			case AbilityScalarValueKind.UInt:
				value = _uintValue;
				return true;
			case AbilityScalarValueKind.String:
				return float.TryParse(_stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
			default:
				value = 0;
				return false;
		}
	}

	public override string ToString() => Convert.ToString(BoxedValue, CultureInfo.InvariantCulture) ?? string.Empty;
}
