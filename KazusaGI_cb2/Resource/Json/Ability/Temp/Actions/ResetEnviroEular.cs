using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class ResetEnviroEular : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public object? enviroSettings { get; set; }
        
        [JsonProperty]
        public bool resetToDefault { get; set; }
        
        [JsonProperty]
        public float duration { get; set; }
    }
}