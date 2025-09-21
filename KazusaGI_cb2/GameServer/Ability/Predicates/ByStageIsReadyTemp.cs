using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    public class ByStageIsReadyTemp : BasePredicate
    {
        [JsonProperty("stage")]
        public string? Stage { get; set; }
        
        [JsonProperty("isReady")]
        public bool IsReady { get; set; }
        
        [JsonProperty("target")]
        public string? Target { get; set; }
    }
}