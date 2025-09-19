using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ForceUseSkillSuccess : BaseAction
    {
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly string type;
    }
}
