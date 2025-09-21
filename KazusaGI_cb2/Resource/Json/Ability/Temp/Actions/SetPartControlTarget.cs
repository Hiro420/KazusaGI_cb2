using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class SetPartControlTarget : BaseAction
{
    [JsonProperty] public string? partName { get; set; }
    [JsonProperty] public string? targetType { get; set; }
    [JsonProperty] public string? targetKey { get; set; }
}