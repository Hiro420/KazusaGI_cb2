using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AttackReviveEnergyMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string[] attackTags;
        [JsonProperty] public readonly float maxValue;
        [JsonProperty] public readonly float minValue;
        [JsonProperty] public readonly float addValue;
        [JsonProperty] public readonly BaseAction reviveAction;
        [JsonProperty] public readonly Dictionary<ElementType, BaseAction> fireEffectActions;
    }
}
