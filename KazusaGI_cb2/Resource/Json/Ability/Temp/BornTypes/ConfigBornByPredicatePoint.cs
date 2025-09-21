using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    internal class ConfigBornByPredicatePoint : BaseBornType
    {
        [JsonProperty] public readonly BasePredicate predicate;
        [JsonProperty] public readonly string predicateKey;
    }
}