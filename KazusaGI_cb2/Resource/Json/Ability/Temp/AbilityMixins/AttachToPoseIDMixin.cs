using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AttachToPoseIDMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly int poseID;
        [JsonProperty] public readonly string modifierName;
    }
}