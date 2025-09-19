using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByTargetGlobalValue : BasePredicate
    {
        [JsonProperty] public readonly string key;
        [JsonProperty] public readonly object value;
        [JsonProperty] public readonly bool forceByCaster;
    }
}
