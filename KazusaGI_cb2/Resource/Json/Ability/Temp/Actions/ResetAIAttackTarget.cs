using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class ResetAIAttackTarget : BaseAction
    {
        [JsonProperty("target")]
        public object Target { get; set; } = new object();

        [JsonProperty("resetType")]
        public string ResetType { get; set; } = string.Empty;
    }
}