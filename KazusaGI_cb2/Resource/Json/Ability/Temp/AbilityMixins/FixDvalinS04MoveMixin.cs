using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class FixDvalinS04MoveMixin : BaseAbilityMixin
{
    [JsonProperty] public string? fixedDirection { get; set; }
    [JsonProperty] public float? fixedSpeed { get; set; }
    [JsonProperty] public bool? useAbsolute { get; set; }
}