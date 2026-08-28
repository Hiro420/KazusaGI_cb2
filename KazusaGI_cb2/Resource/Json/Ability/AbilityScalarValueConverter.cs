using Newtonsoft.Json;
using System.Globalization;

namespace KazusaGI_cb2.Resource.Json.Ability;

internal sealed class AbilityScalarValueConverter : JsonConverter<AbilityScalarValue>
{
	public override AbilityScalarValue ReadJson(JsonReader reader, Type objectType, AbilityScalarValue existingValue,
		bool hasExistingValue, JsonSerializer serializer)
	{
		// Mirrors data::DynamicArgument::fromJson in hk4e *with JsonCpp 1.8.4 semantics*:
		// Json::Value::isDouble() is true for intValue, uintValue and realValue,
		// so every numeric JSON token is stored as float before the isInt() branch
		// can ever be reached. bool stays bool; everything else becomes string.
		return reader.TokenType switch
		{
			JsonToken.Float => AbilityScalarValue.FromFloat(Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture)),
			JsonToken.Integer => AbilityScalarValue.FromFloat(Convert.ToSingle(reader.Value, CultureInfo.InvariantCulture)),
			JsonToken.Boolean => AbilityScalarValue.FromBool(Convert.ToBoolean(reader.Value, CultureInfo.InvariantCulture)),
			JsonToken.String => AbilityScalarValue.FromString(Convert.ToString(reader.Value, CultureInfo.InvariantCulture) ?? string.Empty),
			JsonToken.Null => AbilityScalarValue.FromString(string.Empty),
			_ => AbilityScalarValue.FromString(Convert.ToString(reader.Value, CultureInfo.InvariantCulture) ?? string.Empty),
		};
	}

	public override void WriteJson(JsonWriter writer, AbilityScalarValue value, JsonSerializer serializer)
	{
		switch (value.Kind)
		{
			case AbilityScalarValueKind.Float:
				writer.WriteValue(value.FloatValue);
				break;
			case AbilityScalarValueKind.Int:
				writer.WriteValue(value.IntValue);
				break;
			case AbilityScalarValueKind.UInt:
				writer.WriteValue(value.UIntValue);
				break;
			case AbilityScalarValueKind.Bool:
				writer.WriteValue(value.BoolValue);
				break;
			case AbilityScalarValueKind.String:
				writer.WriteValue(value.StringValue);
				break;
			default:
				throw new JsonSerializationException($"Unsupported ability scalar kind {value.Kind}.");
		}
	}
}
