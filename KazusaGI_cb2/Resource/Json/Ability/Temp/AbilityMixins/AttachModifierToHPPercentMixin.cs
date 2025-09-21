using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AttachModifierToHPPercentMixin : BaseAbilityMixin
{
    [JsonProperty] public float? hpRatio;
    [JsonProperty] public string? modifierName;
}