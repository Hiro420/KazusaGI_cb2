using Newtonsoft.Json.Serialization;

namespace KazusaGI_cb2.Resource.Json.Ability;

/// <summary>
/// Fallback for legacy object-typed fields that carry $type. Strongly typed
/// ability families are handled by AbilityPolymorphicConverter instead.
/// </summary>
internal sealed class AbilitySerializationBinder : ISerializationBinder
{
	public Type BindToType(string? assemblyName, string typeName)
		=> AbilityTypeRegistry.Shared.ResolveGlobal(typeName);

	public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
	{
		assemblyName = null;
		typeName = serializedType.Name;
	}
}
