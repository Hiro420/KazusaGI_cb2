using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SetWeaponBindState : BaseAction
    {
        [JsonProperty("isBindState")]
        public bool IsBindState { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}