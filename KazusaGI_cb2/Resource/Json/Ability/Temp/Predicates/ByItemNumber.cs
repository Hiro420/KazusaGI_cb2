using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByItemNumber : BasePredicate
    {
        [JsonProperty] public readonly int itemId;
        [JsonProperty] public readonly int itemNum;
    }
}
