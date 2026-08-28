using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByHasAbilityState : BasePredicate
{
	[JsonProperty] public readonly string target;
	[JsonProperty] public readonly string abilityState;
}
