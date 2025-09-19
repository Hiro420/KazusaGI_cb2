using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByIsTargetCamp : BasePredicate
{
    [JsonProperty] public readonly string campBaseOn;
    [JsonProperty] public readonly TargetType campTargetType;
}
