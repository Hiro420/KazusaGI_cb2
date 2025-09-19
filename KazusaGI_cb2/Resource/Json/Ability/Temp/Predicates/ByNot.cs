using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByNot : BasePredicate
    {
        [JsonProperty] public readonly BasePredicate[] predicates;
    }
}
