using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByHitBoxType : BasePredicate
    {
        [JsonProperty] public readonly string hitBoxType;
    }
}
