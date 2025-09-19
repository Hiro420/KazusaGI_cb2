using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

public class AttachModifierToPredicateMixin : BaseAbilityMixin
{
    [JsonProperty] public readonly AvatarEventType onEvent;
    [JsonProperty] public readonly BasePredicate[] predicates;
    [JsonProperty] public readonly string modifierName;
}
