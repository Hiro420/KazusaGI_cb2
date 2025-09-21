using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ByHitImpulse : BasePredicate
    {
        [JsonProperty]
        public float impulseThreshold { get; set; }
        
        [JsonProperty]
        public string? compareType { get; set; }
        
        [JsonProperty]
        public bool checkDirection { get; set; }
        
        [JsonProperty]
        public object? directionFilter { get; set; }
    }
}