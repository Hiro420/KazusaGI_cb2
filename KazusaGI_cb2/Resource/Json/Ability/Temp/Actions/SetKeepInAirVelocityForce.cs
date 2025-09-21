using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SetKeepInAirVelocityForce : BaseAction
    {
        [JsonProperty("keepVelocityForce")]
        public object KeepVelocityForce { get; set; } = new object();

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}