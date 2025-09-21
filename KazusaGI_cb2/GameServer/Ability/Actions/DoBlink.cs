using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class DoBlink : BaseAction
    {
        [JsonProperty("distance")]
        public float Distance { get; set; }
        
        [JsonProperty("direction")]
        public string? Direction { get; set; }
        
        [JsonProperty("target")]
        public string? Target { get; set; }
        
        [JsonProperty("useAutoDirection")]
        public bool UseAutoDirection { get; set; }
    }
}