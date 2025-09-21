using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class ShowScreenEffect : BaseAction
    {
        [JsonProperty("effectType")]
        public string? EffectType { get; set; }
        
        [JsonProperty("duration")]
        public float Duration { get; set; }
        
        [JsonProperty("fadeInTime")]
        public float FadeInTime { get; set; }
        
        [JsonProperty("fadeOutTime")]
        public float FadeOutTime { get; set; }
        
        [JsonProperty("color")]
        public string? Color { get; set; }
    }
}