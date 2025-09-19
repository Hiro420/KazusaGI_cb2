using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class CopyGlobalValue : BaseAction
    {
        [JsonProperty] public readonly string dstTarget;
        [JsonProperty] public readonly string srcKey;
        [JsonProperty] public readonly string dstKey;
    }
}
