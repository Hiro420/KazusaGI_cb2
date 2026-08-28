using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public sealed class ConfigAbilityContainer
{
	// The Default payload is intentionally declared through the base family.
	// AbilityPolymorphicConverter supplies ConfigAbility when the JSON object is
	// untagged, matching hk4e's schema-known default factory.
	[JsonProperty("Default", Required = Required.Always)]
	public BaseConfigAbility Default { get; private set; } = null!;
}
