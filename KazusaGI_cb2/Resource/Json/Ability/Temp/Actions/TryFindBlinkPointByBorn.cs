using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class TryFindBlinkPointByBorn : BaseAction
    {
        [JsonProperty("born")]
        public object Born { get; set; } = new object();

        [JsonProperty("blinkRadius")]
        public float BlinkRadius { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}