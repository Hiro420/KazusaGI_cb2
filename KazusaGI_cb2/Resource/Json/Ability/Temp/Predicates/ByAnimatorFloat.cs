using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

internal class ByAnimatorFloat : BasePredicate
{
    [JsonProperty] public string? animatorParameterName;
    [JsonProperty] public float? compareValue;
    [JsonProperty] public string? compareType;
}