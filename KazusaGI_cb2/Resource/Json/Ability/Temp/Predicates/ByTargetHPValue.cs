using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByTargetHPValue : BasePredicate
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly LogicType logic;
        [JsonProperty] public readonly float HP;
    }
}
