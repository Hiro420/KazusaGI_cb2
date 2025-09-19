using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ModifyDamageMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string[] animEventNames;
        [JsonProperty] public readonly object? bonusCriticalHurt;
        [JsonProperty] public readonly object damagePercentageRatio;
    }
}
