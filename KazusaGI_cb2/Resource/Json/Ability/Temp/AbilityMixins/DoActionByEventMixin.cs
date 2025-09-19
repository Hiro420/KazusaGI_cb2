using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByEventMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly AvatarEventType onEvent;
        [JsonProperty] public readonly string type;
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly BaseAction[] actions;
    }
}