using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByKillingMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly double detectWindow;
        [JsonProperty] public readonly BaseAction[] onKill;
    }
}
