using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.SelectTargetType;

internal class SelectTargetsBySelfGroup : BaseSelectTargetType
{
    [JsonProperty] public string? groupType { get; set; }
    [JsonProperty] public bool? includeTeammates { get; set; }
    [JsonProperty] public bool? includeSelf { get; set; }
}