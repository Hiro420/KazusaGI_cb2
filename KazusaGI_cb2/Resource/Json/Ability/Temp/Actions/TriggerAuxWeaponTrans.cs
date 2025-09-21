using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class TriggerAuxWeaponTrans : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public bool enable { get; set; }
        
        [JsonProperty]
        public string? weaponType { get; set; }
        
        [JsonProperty]
        public string? transType { get; set; }
    }
}