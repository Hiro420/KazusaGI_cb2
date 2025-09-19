using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetOverrideMapValue : BaseAction
    {
        [JsonProperty] public readonly BaseSelectTargetType otherTargets;
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly object value;
        [JsonProperty] public readonly string overrideMapKey;
    }
}
