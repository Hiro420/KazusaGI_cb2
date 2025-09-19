using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

public class SkillButtonHoldChargeMixin : BaseAbilityMixin
{
    [JsonProperty] public readonly int overtime;
    [JsonProperty] public readonly string nextLoopTriggerID;
    [JsonProperty] public readonly string endHoldTrigger;
    [JsonProperty] public readonly string[] beforeStateIDs;
    [JsonProperty] public readonly string[] chargeLoopStateIDs;
    [JsonProperty] public readonly float[] chargeLoopDurations;
}
