using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class TryFindBlinkPoint : BaseAction
{
    [JsonProperty] public string? pointType { get; set; }
    [JsonProperty] public float? searchRadius { get; set; }
    [JsonProperty] public bool? preferBehindTarget { get; set; }
}