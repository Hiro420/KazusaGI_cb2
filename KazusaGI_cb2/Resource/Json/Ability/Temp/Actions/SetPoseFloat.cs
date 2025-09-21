using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SetPoseFloat : BaseAction
    {
        [JsonProperty]
        public string? poseFloatName { get; set; }
        
        [JsonProperty]
        public float value { get; set; }
        
        [JsonProperty]
        public string? target { get; set; }
    }
}