using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByAnimatorBool : BasePredicate
    {
        [JsonProperty] public readonly bool value;
        [JsonProperty] public readonly string parameter;
    }
}
