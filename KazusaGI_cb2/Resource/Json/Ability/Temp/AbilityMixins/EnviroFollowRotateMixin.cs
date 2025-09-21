using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class EnviroFollowRotateMixin : BaseAbilityMixin
{
    [JsonProperty] public string? targetType { get; set; }
    [JsonProperty] public bool? followRotation { get; set; }
    [JsonProperty] public float? rotationSpeed { get; set; }
}