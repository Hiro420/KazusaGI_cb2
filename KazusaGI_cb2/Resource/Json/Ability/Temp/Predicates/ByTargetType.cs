using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByTargetType : BasePredicate
    {
        [JsonProperty] public readonly bool isTarget;
    }
}
