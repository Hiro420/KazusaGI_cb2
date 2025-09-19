using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class OnAvatarUseSkillMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly BaseAction[] onTriggerSkill;
        [JsonProperty] public readonly float clearSkillIdDelay;
    }
}
