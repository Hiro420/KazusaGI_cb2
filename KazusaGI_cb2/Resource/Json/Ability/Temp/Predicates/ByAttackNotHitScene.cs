using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByAttackNotHitScene : BasePredicate
    {
        [JsonProperty] public readonly BaseAttackPattern attackPattern;
    }
}
