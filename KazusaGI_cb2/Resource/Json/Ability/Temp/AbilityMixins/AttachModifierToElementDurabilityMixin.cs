using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AttachModifierToElementDurabilityMixin : BaseAbilityMixin
{
    [JsonProperty] public string? elementType;
    [JsonProperty] public string? modifierName;
}