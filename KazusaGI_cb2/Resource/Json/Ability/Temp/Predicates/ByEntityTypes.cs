using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
	internal class ByEntityTypes : BasePredicate
	{
		[JsonProperty] public readonly EntityType[] entityTypes;
	}
}
