using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerAudio : BaseAction
    {
        [JsonProperty] public readonly BaseEventOp operation;
    }
}
