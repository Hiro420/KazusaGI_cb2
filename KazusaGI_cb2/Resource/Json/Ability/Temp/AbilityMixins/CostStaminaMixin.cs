using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class CostStaminaMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly object costStaminaDelta;
        [JsonProperty] public readonly BaseAction[] onStaminaEmpty;
    }
}
