using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class AvatarExitClimb : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public bool forceExit { get; set; }
        
        [JsonProperty]
        public string? exitType { get; set; }
    }
}