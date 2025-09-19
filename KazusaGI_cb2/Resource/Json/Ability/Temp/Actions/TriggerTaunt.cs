using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerTaunt : BaseAction
    {
        [JsonProperty] public readonly BaseSelectTargetType otherTargets;
        [JsonProperty] public readonly BasePredicate[] predicates;
    }
}
