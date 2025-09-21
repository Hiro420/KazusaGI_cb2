using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes;

internal class ConfigBornByWorld : BaseBornType
{
    [JsonProperty] public string? position { get; set; }
    [JsonProperty] public string? rotation { get; set; }
    [JsonProperty] public bool? useGroundHeight { get; set; }
}