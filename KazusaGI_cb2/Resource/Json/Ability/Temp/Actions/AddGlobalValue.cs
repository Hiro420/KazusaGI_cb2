using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AddGlobalValue : BaseAction
    {
        [JsonProperty] public readonly object value;
        [JsonProperty] public readonly string key;
        [JsonProperty] public readonly bool useLimitRange;
        [JsonProperty] public readonly object maxValue;
        [JsonProperty] public readonly object minValue;
    }
}
