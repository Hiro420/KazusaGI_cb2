using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ExtendLifetimeByPickedGadgetMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly int[] pickedConfigIDs;
        [JsonProperty] public readonly string extendLifeTime;
        [JsonProperty] public readonly string maxExtendLifeTime;
    }
}
