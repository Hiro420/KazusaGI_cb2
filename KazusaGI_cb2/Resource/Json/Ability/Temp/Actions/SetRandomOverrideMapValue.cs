using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetRandomOverrideMapValue : BaseAction
    {
        [JsonProperty] public readonly float valueRangeMax;
        [JsonProperty] public readonly string overrideMapKey;
    }
}
