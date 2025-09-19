using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByGainCrystalSeedMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly ElementType[] elementTypes;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly BaseAction[] actions;
    }
}
