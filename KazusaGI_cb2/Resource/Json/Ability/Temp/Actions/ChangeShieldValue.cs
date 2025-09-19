using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ChangeShieldValue : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly object shieldHPRatio;
        [JsonProperty] public readonly object shieldHP;
        [JsonProperty] public readonly object? maxShieldByHPRatio;
        [JsonProperty] public readonly object? maxShieldHP;
        [JsonProperty] public readonly bool doOffStage;
    }
}
