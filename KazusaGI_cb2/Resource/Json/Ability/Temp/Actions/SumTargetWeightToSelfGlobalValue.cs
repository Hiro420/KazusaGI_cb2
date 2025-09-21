using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SumTargetWeightToSelfGlobalValue : BaseAction
    {
        [JsonProperty("globalValueKey")]
        public string GlobalValueKey { get; set; } = string.Empty;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();

        [JsonProperty("weightMultiplier")]
        public float WeightMultiplier { get; set; } = 1.0f;
    }
}