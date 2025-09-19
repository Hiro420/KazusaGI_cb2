using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AttachModifierToSelfGlobalValueMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string globalValueKey;
        [JsonProperty] public readonly float defaultGlobalValueOnCreate;
        [JsonProperty] public readonly float[] valueSteps;
        [JsonProperty] public readonly string[] modifierNameSteps;
    }
}
