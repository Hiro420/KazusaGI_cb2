using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SetGlobalValueByTargetDistance : BaseAction
    {
        [JsonProperty("globalValueKey")]
        public string GlobalValueKey { get; set; } = string.Empty;

        [JsonProperty("targetDistance")]
        public float TargetDistance { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}