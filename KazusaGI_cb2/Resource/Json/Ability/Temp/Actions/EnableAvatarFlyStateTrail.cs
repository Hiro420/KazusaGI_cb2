using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class EnableAvatarFlyStateTrail : BaseAction
    {
        [JsonProperty("enable")]
        public bool Enable { get; set; }

        [JsonProperty("trailEffect")]
        public string TrailEffect { get; set; } = string.Empty;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}