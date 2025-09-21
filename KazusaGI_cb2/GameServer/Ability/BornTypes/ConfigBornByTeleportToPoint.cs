using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    public class ConfigBornByTeleportToPoint : BaseBornType
    {
        [JsonProperty("pointName")]
        public string? PointName { get; set; }
        
        [JsonProperty("target")]
        public string? Target { get; set; }
        
        [JsonProperty("offset")]
        public object? Offset { get; set; }
        
        [JsonProperty("useGroundOffset")]
        public bool UseGroundOffset { get; set; }
    }
}