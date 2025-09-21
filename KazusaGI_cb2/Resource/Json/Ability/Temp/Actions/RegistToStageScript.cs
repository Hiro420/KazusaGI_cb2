using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class RegistToStageScript : BaseAction
    {
        [JsonProperty("stageName")]
        public string StageName { get; set; } = string.Empty;

        [JsonProperty("scriptKey")]
        public string ScriptKey { get; set; } = string.Empty;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}