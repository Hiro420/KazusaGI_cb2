using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

/// <summary>
/// Server runtime counterpart of hk4e ActorAbility.
/// The protocol instance id and override map are persistent runtime state; they are
/// never reconstructed opportunistically while serializing AbilitySyncStateInfo.
/// </summary>
public sealed class ActorAbility
{
	public uint AbilityId { get; internal set; }
	public uint AbilityNameHash { get; }
	public Entity Caster { get; }
	public ConfigAbility Config { get; }
	public string OverrideName { get; }
	public uint OverrideNameHash { get; }

	// hk4e ActorAbility::override_map_: std::map<int, std::any>.
	// AbilityScalarValue preserves the concrete scalar type instead of normalizing
	// the entire runtime to float.
	public SortedDictionary<int, AbilityScalarValue> OverrideMap { get; } = new();

	public bool ArgumentReceived { get; set; }
	public float ArgumentSpecialValue { get; set; }

	public ActorAbility(
		uint abilityId,
		Entity caster,
		ConfigAbility config,
		string? overrideName = null,
		uint overrideNameHash = 0)
	{
		AbilityId = abilityId;
		Caster = caster ?? throw new ArgumentNullException(nameof(caster));
		Config = config ?? throw new ArgumentNullException(nameof(config));
		AbilityNameHash = Utils.AbilityHash(config.abilityName);
		OverrideName = string.IsNullOrWhiteSpace(overrideName) ? "Default" : overrideName;
		OverrideNameHash = overrideNameHash != 0 ? overrideNameHash : Utils.AbilityHash(OverrideName);
	}

	public void LoadOverrideMap(IEnumerable<AbilityScalarValueEntry> entries)
	{
		foreach (AbilityScalarValueEntry entry in entries)
		{
			if (entry?.Key == null)
				continue;

			uint keyHash = entry.Key.Hash != 0
				? entry.Key.Hash
				: (!string.IsNullOrEmpty(entry.Key.Str) ? Utils.AbilityHash(entry.Key.Str) : 0u);
			if (keyHash == 0)
				continue;

			AbilityScalarValue value;
			switch (entry.ValueType)
			{
				case AbilityScalarType.AbilityScalarTypeFloat:
					value = AbilityScalarValue.FromFloat(entry.FloatValue);
					break;
				case AbilityScalarType.AbilityScalarTypeInt:
					value = AbilityScalarValue.FromInt(entry.IntValue);
					break;
				case AbilityScalarType.AbilityScalarTypeUint:
					value = AbilityScalarValue.FromUInt(entry.UintValue);
					break;
				case AbilityScalarType.AbilityScalarTypeString:
					value = AbilityScalarValue.FromString(entry.StringValue);
					break;
				default:
					continue;
			}

			OverrideMap[unchecked((int)keyHash)] = value;
		}
	}

	public AbilityAppliedAbility ToProtocol()
	{
		var result = new AbilityAppliedAbility
		{
			AbilityName = new AbilityString { Hash = AbilityNameHash },
			InstancedAbilityId = AbilityId
		};

		if (!string.Equals(OverrideName, "Default", StringComparison.Ordinal))
			result.AbilityOverride = new AbilityString { Hash = OverrideNameHash };

		foreach (var kvp in OverrideMap)
		{
			AbilityScalarValue value = kvp.Value;
			var entry = new AbilityScalarValueEntry
			{
				Key = new AbilityString { Hash = unchecked((uint)kvp.Key) }
			};

			switch (value.Kind)
			{
				case AbilityScalarValueKind.Float:
					entry.ValueType = AbilityScalarType.AbilityScalarTypeFloat;
					entry.FloatValue = value.FloatValue;
					break;
				case AbilityScalarValueKind.Int:
					entry.ValueType = AbilityScalarType.AbilityScalarTypeInt;
					entry.IntValue = value.IntValue;
					break;
				case AbilityScalarValueKind.UInt:
					entry.ValueType = AbilityScalarType.AbilityScalarTypeUint;
					entry.UintValue = value.UIntValue;
					break;
				case AbilityScalarValueKind.String:
					entry.ValueType = AbilityScalarType.AbilityScalarTypeString;
					entry.StringValue = value.StringValue;
					break;
				default:
					continue;
			}

			result.OverrideMaps.Add(entry);
		}

		return result;
	}
}
