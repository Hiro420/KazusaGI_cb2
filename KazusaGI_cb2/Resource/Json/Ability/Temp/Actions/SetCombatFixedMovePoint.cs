using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SetCombatFixedMovePoint : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public object? movePoint { get; set; }
        
        [JsonProperty]
        public bool enable { get; set; }
        
        [JsonProperty]
        public string? pointName { get; set; }
    }
}