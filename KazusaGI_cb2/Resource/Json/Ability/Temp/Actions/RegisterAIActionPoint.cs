using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class RegisterAIActionPoint : BaseAction
{
    [JsonProperty] public string? actionPointType { get; set; }
    [JsonProperty] public string? pointKey { get; set; }
    [JsonProperty] public bool? overrideExisting { get; set; }
}