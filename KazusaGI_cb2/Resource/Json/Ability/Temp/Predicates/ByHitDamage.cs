using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByHitDamage : BasePredicate
    {
        [JsonProperty] public readonly float damage;
    }
}
