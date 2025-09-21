using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AttachToAnimatorStateMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string stateIDs;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly float normalizeStart;
        [JsonProperty] public readonly float normalizeEnd;
    }
}