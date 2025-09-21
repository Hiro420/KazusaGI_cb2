using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PushDvalinS01Process : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public int processStep { get; set; }
        
        [JsonProperty]
        public string? processType { get; set; }
        
        [JsonProperty]
        public object? processData { get; set; }
    }
}