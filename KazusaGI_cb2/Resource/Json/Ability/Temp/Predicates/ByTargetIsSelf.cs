using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByTargetIsSelf : BasePredicate
    {
        [JsonProperty] public readonly bool isSelf;
    }
}
