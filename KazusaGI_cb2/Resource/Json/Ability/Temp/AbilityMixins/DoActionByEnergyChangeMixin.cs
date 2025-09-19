using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByEnergyChangeMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly ElementType[] elementTypes;
        [JsonProperty] public readonly bool doWhenEnergyMax;
        [JsonProperty] public readonly BaseAction[] onGainEnergyByBall;
    }
}
