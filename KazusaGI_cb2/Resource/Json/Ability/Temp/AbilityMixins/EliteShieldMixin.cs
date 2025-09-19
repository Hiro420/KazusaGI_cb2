using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class EliteShieldMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string shieldType;
        [JsonProperty] public readonly float shieldAngle;
        [JsonProperty] public readonly object shieldHPRatio;
        [JsonProperty] public readonly object shieldHP;
        [JsonProperty] public readonly string showDamageText;
        [JsonProperty] public readonly BaseAction[] onShieldBroken;
    }
}
