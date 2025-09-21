using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SetEntityScale : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public object? scale { get; set; }
        
        [JsonProperty]
        public float duration { get; set; }
        
        [JsonProperty]
        public string? scaleType { get; set; }
    }
}