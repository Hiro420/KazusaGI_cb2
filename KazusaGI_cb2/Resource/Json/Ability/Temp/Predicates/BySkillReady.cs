using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class BySkillReady : BasePredicate
    {
        [JsonProperty] public readonly int overtime;
    }
}
