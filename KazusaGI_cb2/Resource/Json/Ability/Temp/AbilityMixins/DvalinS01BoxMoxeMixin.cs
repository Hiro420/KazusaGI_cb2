using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    [JsonObject(MemberSerialization.OptIn)]
    public class DvalinS01BoxMoxeMixin : BaseAbilityMixin
    {
        [JsonProperty]
        public object? moveConfig { get; set; }
        
        [JsonProperty]
        public string? moveType { get; set; }
        
        [JsonProperty]
        public float duration { get; set; }
        
        [JsonProperty]
        public object? boxSettings { get; set; }
    }
}