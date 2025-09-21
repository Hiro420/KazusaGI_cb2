using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SyncToStageScript : BaseAction
    {
        [JsonProperty("stageName")]
        public string StageName { get; set; } = string.Empty;

        [JsonProperty("scriptKey")]
        public string ScriptKey { get; set; } = string.Empty;

        [JsonProperty("syncData")]
        public object SyncData { get; set; } = new object();

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}