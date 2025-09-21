using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ElementAdjustMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly float elementDurabilityRatio;
        [JsonProperty] public readonly string elementType;
    }
}