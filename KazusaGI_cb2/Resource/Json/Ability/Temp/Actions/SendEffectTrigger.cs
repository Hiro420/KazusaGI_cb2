using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SendEffectTrigger : BaseAction
    {
        [JsonProperty] public readonly string parameter;
        [JsonProperty] public readonly string effectPattern;
    }
}
