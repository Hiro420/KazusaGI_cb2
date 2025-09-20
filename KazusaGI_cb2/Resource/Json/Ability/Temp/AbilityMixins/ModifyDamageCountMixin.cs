using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ModifyDamageCountMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string[] attackTags;
        [JsonProperty] public readonly object damageExtra;
        [JsonProperty] public readonly object maxModifyCount;
        [JsonProperty] public readonly BaseAction[] successActions;
        [JsonProperty] public readonly BaseAction[] maxCountActions;
        [JsonProperty] public readonly BasePredicate[] predicates;
    }
}
