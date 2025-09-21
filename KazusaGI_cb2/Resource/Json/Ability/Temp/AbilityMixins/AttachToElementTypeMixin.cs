using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AttachToElementTypeMixin : BaseAbilityMixin
{
    [JsonProperty] public string? elementType;
    [JsonProperty] public string? modifierName;
}