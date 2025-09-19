using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class WindZoneMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string shapeName;
        [JsonProperty] public readonly BaseBornType born;
        [JsonProperty] public readonly object strength;
        [JsonProperty] public readonly object attenuation;
        [JsonProperty] public readonly float innerRadius;
        [JsonProperty] public readonly TargetType targetType;
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly string modifierName;
    }
}
