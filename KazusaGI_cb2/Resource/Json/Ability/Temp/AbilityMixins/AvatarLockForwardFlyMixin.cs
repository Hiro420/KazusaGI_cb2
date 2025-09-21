using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AvatarLockForwardFlyMixin : BaseAbilityMixin
{
    [JsonProperty] public bool? lockForward;
    [JsonProperty] public float? duration;
}