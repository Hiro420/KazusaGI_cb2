using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class RemoveAvatarSkillInfo : BaseAction
    {
        [JsonProperty] public readonly int overtime;
    }
}
