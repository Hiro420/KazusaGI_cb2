using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByHitBoxName : BasePredicate
    {
        [JsonProperty] public readonly string[] hitBoxNames;
    }
}
