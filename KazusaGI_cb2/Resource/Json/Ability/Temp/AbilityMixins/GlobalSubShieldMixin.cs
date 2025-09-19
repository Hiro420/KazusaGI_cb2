using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class GlobalSubShieldMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string mainShieldModifierName;
    }
}
