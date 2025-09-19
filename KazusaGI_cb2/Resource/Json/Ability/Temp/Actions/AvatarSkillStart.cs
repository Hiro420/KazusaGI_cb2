using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AvatarSkillStart : BaseAction
    {
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly object cdRatio;
        [JsonProperty] public readonly object costStaminaRatio;
    }
}
