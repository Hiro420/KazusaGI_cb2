using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ElementHittingOtherPredicatedMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly BasePredicate[] prePredicates;
        [JsonProperty] public readonly ElementPredicateds[] elementBatchPredicateds;

        public class ElementPredicateds
        {
            [JsonProperty] public readonly ElementType[] elementTypeArr;
            [JsonProperty] public readonly BaseAction[] successActions;
        }
    }
}
