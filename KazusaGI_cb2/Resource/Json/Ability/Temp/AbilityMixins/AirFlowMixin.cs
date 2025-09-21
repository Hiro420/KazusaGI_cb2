using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AirFlowMixin : BaseAbilityMixin
{
    [JsonProperty] public float? windStrength;
    [JsonProperty] public float[]? direction;
}