using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class SwitchSkillIDMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string priority;
        [JsonProperty] public readonly int skillIndex;
        [JsonProperty] public readonly int overtime;
    }
}
