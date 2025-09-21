using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    [JsonObject(MemberSerialization.OptIn)]
    public class WeightDetectRegionMixin : BaseAbilityMixin
    {
        [JsonProperty]
        public object? detectionRegion { get; set; }
        
        [JsonProperty]
        public float weightThreshold { get; set; }
        
        [JsonProperty]
        public string? detectType { get; set; }
        
        [JsonProperty]
        public bool enableWeightCalculation { get; set; }
    }
}