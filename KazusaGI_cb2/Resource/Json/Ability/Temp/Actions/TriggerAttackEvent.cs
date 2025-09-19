using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class TriggerAttackEvent : BaseAction
{
    [JsonProperty] public readonly TargetType targetType;
    [JsonProperty] public readonly bool doOffStage;
    [JsonProperty] public readonly AttackEvent attackEvent;

    public class AttackEvent
    {
        [JsonProperty] public readonly BaseAttackPattern attackPattern;
        [JsonProperty] public readonly AttackInfo attackInfo;
    }
}
