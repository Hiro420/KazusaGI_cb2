using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByAttackType : BasePredicate
    {
        [JsonProperty] public readonly string attackType;
    }
}
