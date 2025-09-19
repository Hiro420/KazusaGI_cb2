using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByTargetOverrideMapValue : BasePredicate
{
    [JsonProperty] public readonly LogicType logic;
    [JsonProperty] public readonly string targetAbilityName;
    [JsonProperty] public readonly string targetKey;
    [JsonProperty] public readonly object targetValue;
}
