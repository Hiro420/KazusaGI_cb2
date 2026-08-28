using KazusaGI_cb2.Resource.Json.Ability.Temp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KazusaGI_cb2.Resource.Json.Ability;

/// <summary>
/// Context-sensitive equivalent of hk4e's per-base-class JSON factory maps.
/// A simple $type name is resolved against the declared base family.
/// </summary>
internal sealed class AbilityPolymorphicConverter : JsonConverter
{
	public override bool CanWrite => false;

	public override bool CanConvert(Type objectType)
		=> AbilityTypeRegistry.Shared.IsPolymorphicRoot(objectType);

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;

		JObject obj;
		try
		{
			obj = JObject.Load(reader);
		}
		catch (Exception e)
		{
			throw new JsonSerializationException(
				$"Expected JSON object for polymorphic ability type '{objectType.Name}' at path '{reader.Path}'.", e);
		}

		string? jsonTypeName = obj["$type"]?.Value<string>();
		Type targetType;

		if (string.IsNullOrWhiteSpace(jsonTypeName))
		{
			// Ability files use an untagged Default object. hk4e knows this is
			// a ConfigAbility from the container schema; mirror that explicitly.
			if (objectType == typeof(BaseConfigAbility))
				targetType = typeof(ConfigAbility);
			else if (!objectType.IsAbstract && !objectType.IsInterface)
				targetType = objectType;
			else
				throw new JsonSerializationException(
					$"Missing $type for abstract ability JSON family '{objectType.Name}' at path '{reader.Path}'.");
		}
		else
		{
			targetType = AbilityTypeRegistry.Shared.Resolve(objectType, jsonTypeName);
		}

		object instance;
		try
		{
			instance = Activator.CreateInstance(targetType, nonPublic: true)
				?? throw new JsonSerializationException($"Could not instantiate '{targetType.FullName}'.");
		}
		catch (Exception e) when (e is not JsonSerializationException)
		{
			throw new JsonSerializationException($"Could not instantiate ability JSON type '{targetType.FullName}'.", e);
		}

		obj.Remove("$type");
		using JsonReader objectReader = obj.CreateReader();
		serializer.Populate(objectReader, instance);
		return instance;
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		=> throw new NotSupportedException();
}
