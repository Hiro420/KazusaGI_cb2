using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class DamageByAttackValue : BaseAction
    {
        [JsonProperty] public readonly string target;
        [JsonProperty] public readonly BaseSelectTargetType? otherTargets;
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly BaseBornType? born;
        [JsonProperty] public readonly AttackInfo attackInfo;
    }
}