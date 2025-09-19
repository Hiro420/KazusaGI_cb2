using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ApplyInertiaVelocityMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly float damping;
        [JsonProperty] public readonly bool useXZ;
        [JsonProperty] public readonly bool useY;
    }
}
