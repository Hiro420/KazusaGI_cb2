using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByPoseIDMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly int poseID;
        [JsonProperty] public readonly BaseAction[] onCall;
    }
}