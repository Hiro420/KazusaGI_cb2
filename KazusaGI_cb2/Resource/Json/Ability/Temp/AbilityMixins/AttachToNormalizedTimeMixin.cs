using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AttachToNormalizedTimeMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string stateID;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly float normalizeStart;
    }
}
