using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetGlobalValueToOverrideMap : BaseAction
    {
        [JsonProperty] public readonly string globalValueKey;
        [JsonProperty] public readonly string overrideMapKey;
    }
}
