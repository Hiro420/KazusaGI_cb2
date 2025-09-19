using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.EventOps
{
    internal class ConfigAudioPositionedEventOp : BaseEventOp
    {
        [JsonProperty] public readonly float positioning;
        [JsonProperty] public readonly float duration;
    }
}
