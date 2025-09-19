using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class FieldEntityCountChangeMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly TargetType campTargetType;
        [JsonProperty] public readonly BasePredicate[] targetPredicates;
        [JsonProperty] public readonly BaseAction[] onFieldEnter;
        [JsonProperty] public readonly BaseAction[] onFieldExit;

    }
}
