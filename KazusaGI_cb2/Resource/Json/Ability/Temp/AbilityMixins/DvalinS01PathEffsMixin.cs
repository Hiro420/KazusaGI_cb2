using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class DvalinS01PathEffsMixin : BaseAbilityMixin
{
    [JsonProperty] public string? pathType { get; set; }
    [JsonProperty] public string? effectName { get; set; }
    [JsonProperty] public bool? followPath { get; set; }
}