using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ReviveDeadAvatar : BaseAction
    {
        [JsonProperty] public readonly BasePredicate[] predicates;
        [JsonProperty] public readonly float amountByTargetMaxHPRatio;
        [JsonProperty] public readonly bool isReviveOtherPlayerAvatar;
        [JsonProperty] public readonly int overtime;
        [JsonProperty] public readonly float cdRatio;
        [JsonProperty] public readonly float rayCount;
    }
}
