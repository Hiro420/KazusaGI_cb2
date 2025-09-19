using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class FireSubEmitterEffect : BaseAction
    {
        [JsonProperty] public readonly string effectPattern;
        [JsonProperty] public readonly BaseBornType born;
    }
}
