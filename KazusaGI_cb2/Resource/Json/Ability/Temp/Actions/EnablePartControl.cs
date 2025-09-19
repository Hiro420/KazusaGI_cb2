using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnablePartControl : BaseAction
    {
        [JsonProperty] public readonly string[] partRootNames;
        [JsonProperty] public readonly bool enable;
    }
}
