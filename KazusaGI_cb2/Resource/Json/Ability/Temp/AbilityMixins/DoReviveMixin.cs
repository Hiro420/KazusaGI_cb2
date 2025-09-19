using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoReviveMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly BaseAction[] onReviveActions;
    }
}
