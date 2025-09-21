using Newtonsoft.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    [JsonObject(MemberSerialization.OptIn)]
    public class SetWeaponAttachPointRealName : BaseAction
    {
        [JsonProperty]
        public string? target { get; set; }
        
        [JsonProperty]
        public string? attachPointName { get; set; }
        
        [JsonProperty]
        public string? realName { get; set; }
    }
}