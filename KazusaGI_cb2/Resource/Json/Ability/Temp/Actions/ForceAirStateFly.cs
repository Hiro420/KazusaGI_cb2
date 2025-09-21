using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ForceAirStateFly : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public bool enable { get; set; }
        
        [JsonProperty]
        public float duration { get; set; }
        
        [JsonProperty]
        public string? flyState { get; set; }
    }
}