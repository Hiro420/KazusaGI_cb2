using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class TriggerCreateGadgetToEquipPart : BaseAction
    {
        [JsonProperty("gadgetID")]
        public int GadgetID { get; set; }

        [JsonProperty("equipPart")]
        public string EquipPart { get; set; } = string.Empty;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}