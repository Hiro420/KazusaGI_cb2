using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ButtonHoldChargeMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly float chargeTime;
        [JsonProperty] public readonly object secondChargeTime;
        [JsonProperty] public readonly BaseAction[]? OnBeginUncharged;
        [JsonProperty] public readonly BaseAction[]? OnReleaseUncharged;
        [JsonProperty] public readonly BaseAction[]? OnBeginCharged;
        [JsonProperty] public readonly BaseAction[]? OnReleaseCharged;
        [JsonProperty] public readonly BaseAction[]? OnBeginSecondCharged;
        [JsonProperty] public readonly string[] chargeStateIDs;
    }
}
