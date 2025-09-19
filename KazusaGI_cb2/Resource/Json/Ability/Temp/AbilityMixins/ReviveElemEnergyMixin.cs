using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ReviveElemEnergyMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly float period;
        [JsonProperty] public readonly string baseEnergy;
        [JsonProperty] public readonly string ratio;
    }
}
