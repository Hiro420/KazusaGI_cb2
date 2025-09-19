using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByHitEnBreak : BasePredicate
    {
        [JsonProperty] public readonly float enBreak;
    }
}
