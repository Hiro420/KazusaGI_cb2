using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class BoxClampWindZoneMixin : BaseAbilityMixin
{
    [JsonProperty] public float[]? boxSize;
    [JsonProperty] public float? windForce;
}