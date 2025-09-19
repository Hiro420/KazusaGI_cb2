using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ApplyModifier : BaseAction
    {
        [JsonProperty] public readonly string? target;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly BaseSelectTargetType? otherTargets;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly BasePredicate[]? predicates;
    }
}
