using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

internal class ByCurTeamHasElementType : BasePredicate
{
    [JsonProperty] public string? elementType { get; set; }
    [JsonProperty] public int? minCount { get; set; }
    [JsonProperty] public bool? includeLeader { get; set; }
}