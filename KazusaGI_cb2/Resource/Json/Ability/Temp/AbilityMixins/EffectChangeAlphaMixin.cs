using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class EffectChangeAlphaMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly float startDuration;
        [JsonProperty] public readonly float endDuration;
    }
}
