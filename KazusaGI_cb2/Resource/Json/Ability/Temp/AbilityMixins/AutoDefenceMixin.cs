using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

public class AutoDefenceMixin : BaseAbilityMixin
{
    [JsonProperty] public readonly string[] stateIDs;
    [JsonProperty] public readonly TriggerID defendTriggerID;
    [JsonProperty] public readonly float defendProbability;
    [JsonProperty] public readonly float defendProbabilityDelta;
    [JsonProperty] public readonly string defendTimeInterval;
    [JsonProperty] public readonly int defendCountInterval;
}
