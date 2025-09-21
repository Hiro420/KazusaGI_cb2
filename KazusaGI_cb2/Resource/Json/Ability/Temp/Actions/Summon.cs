using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Summon : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public object? summonConfig { get; set; }
        
        [JsonProperty]
        public string? summonType { get; set; }
        
        [JsonProperty]
        public float duration { get; set; }
        
        [JsonProperty]
        public int monsterId { get; set; }
        
        [JsonProperty]
        public object? pos { get; set; }
        
        [JsonProperty]
        public object? rot { get; set; }
    }
}