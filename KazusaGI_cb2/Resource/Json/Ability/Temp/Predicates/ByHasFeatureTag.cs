using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

internal class ByHasFeatureTag : BasePredicate
{
    [JsonProperty] public string? featureTag;
}