using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ModifyAvatarSkillCD : BaseAction
    {
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly float cdRatio;
    }
}
