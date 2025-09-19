using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetTargetNumToGlobalValue : BaseAction
    {
        [JsonProperty] public readonly BaseSelectTargetType srcOtherTargets;
        [JsonProperty] public readonly BasePredicate[] srcPredicates;
        [JsonProperty] public readonly TargetType dstTarget;
        [JsonProperty] public readonly string key;
    }
}
