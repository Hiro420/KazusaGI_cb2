using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TryTriggerPlatformStartMove : BaseAction
    {
        [JsonProperty] public readonly float detectHeight;
        [JsonProperty] public readonly float detectWidth;
        [JsonProperty] public readonly BaseAction[] failActions;
    }
}
