using Newtonsoft.Json;
using KazusaGI_cb2.Resource;
using Newtonsoft.Json.Converters;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByHasAbilityState : BasePredicate
{
    [JsonProperty] public readonly string target;
    [JsonProperty] public readonly string abilityState;
}
