using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class TriggerAttackTargetMapEvent : BaseAction
{
    [JsonProperty] public readonly TargetType target;
    [JsonProperty] public readonly AttackTargetMapEvent attackTargetMapEvent;

    public class AttackTargetMapEvent
    {
        [JsonProperty] public readonly BaseAttackPattern attackPattern;
        [JsonProperty] public readonly Dictionary<TargetType, AttackInfo> attackInfoMap;
    }
}
