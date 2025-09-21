using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class PushPos : BaseAction
    {
        [JsonProperty("pushDir")]
        public object PushDir { get; set; } = new object();

        [JsonProperty("pushForce")]
        public float PushForce { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}