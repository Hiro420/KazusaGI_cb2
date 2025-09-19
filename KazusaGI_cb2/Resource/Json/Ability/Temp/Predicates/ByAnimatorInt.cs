using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByAnimatorInt : BasePredicate
    {
        [JsonProperty] public readonly LogicType logic;
        [JsonProperty] public readonly int value;
        [JsonProperty] public readonly string parameter;
    }
}
