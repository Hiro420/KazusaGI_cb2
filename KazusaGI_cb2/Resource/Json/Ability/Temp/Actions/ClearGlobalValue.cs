using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ClearGlobalValue : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly string key;
    }
}
