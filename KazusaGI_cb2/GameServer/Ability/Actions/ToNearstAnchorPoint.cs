using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class ToNearstAnchorPoint : BaseAction
    {
        [JsonProperty("distance")]
        public float Distance { get; set; }
        
        [JsonProperty("target")]
        public string? Target { get; set; }
        
        [JsonProperty("anchorType")]
        public string? AnchorType { get; set; }
    }
}