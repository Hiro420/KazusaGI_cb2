using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SetDvalinS01FlyState : BaseAction
    {
        [JsonProperty("flyState")]
        public string flyState { get; set; }
        
        [JsonProperty("doOffStage")]
        public bool doOffStage { get; set; }
    }
}