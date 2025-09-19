using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByTargetAltitude : BasePredicate
{
    [JsonProperty] public readonly LogicType? logic;
    [JsonProperty] public readonly float value;
}
