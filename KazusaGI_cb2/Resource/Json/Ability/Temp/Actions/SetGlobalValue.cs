using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetGlobalValue : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly object value;
        [JsonProperty] public readonly string key;
        [JsonProperty] public readonly object maxValue;
        [JsonProperty] public readonly float minValue;
    }
}
