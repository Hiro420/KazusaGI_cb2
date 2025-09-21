using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PlayEmojiBubble : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public string? emojiId { get; set; }
        
        [JsonProperty]
        public float duration { get; set; }
        
        [JsonProperty]
        public object? bubbleConfig { get; set; }
    }
}