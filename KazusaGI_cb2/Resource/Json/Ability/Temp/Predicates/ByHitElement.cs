using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByHitElement : BasePredicate
    {
        [JsonProperty] public readonly ElementType element;
    }
}
