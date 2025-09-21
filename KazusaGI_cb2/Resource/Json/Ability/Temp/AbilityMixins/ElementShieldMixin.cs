using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class ElementShieldMixin : BaseAbilityMixin
{
    [JsonProperty] public string? elementType;
    [JsonProperty] public float? shieldValue;
}