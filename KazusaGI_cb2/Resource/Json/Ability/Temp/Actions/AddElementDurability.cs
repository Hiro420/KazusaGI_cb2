using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AddElementDurability : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly object value;
        [JsonProperty] public readonly BasePredicate[]? predicates;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly bool useLimitRange;
        [JsonProperty] public readonly object maxValue;
        [JsonProperty] public readonly float minValue;
    }
}
