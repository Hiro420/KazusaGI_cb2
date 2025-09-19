using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetGlobalDir : BaseAction
    {
        [JsonProperty] public readonly string key;
        [JsonProperty] public readonly BaseBornType born;
    }
}
