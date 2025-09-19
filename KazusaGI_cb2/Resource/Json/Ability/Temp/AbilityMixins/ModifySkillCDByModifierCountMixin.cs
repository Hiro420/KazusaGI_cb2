using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ModifySkillCDByModifierCountMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly TargetType targetType;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly string cdDelta;
    }
}
