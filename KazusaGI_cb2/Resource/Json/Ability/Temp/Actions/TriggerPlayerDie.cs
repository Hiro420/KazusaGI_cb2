using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class TriggerPlayerDie : BaseAction
    {
        [JsonProperty("target")]
        public object Target { get; set; } = new object();

        [JsonProperty("killType")]
        public string KillType { get; set; } = string.Empty;
    }
}