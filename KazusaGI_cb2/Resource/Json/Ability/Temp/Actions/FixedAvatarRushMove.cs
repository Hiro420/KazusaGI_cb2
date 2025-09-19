using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class FixedAvatarRushMove : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly BaseBornType toPos;
        [JsonProperty] public readonly float timeRange;
        [JsonProperty] public readonly float maxRange;
        [JsonProperty] public readonly string[] animatorStateIDs;
        [JsonProperty] public readonly string overrideMoveCollider;
        [JsonProperty] public readonly bool isInAir;
    }
}
