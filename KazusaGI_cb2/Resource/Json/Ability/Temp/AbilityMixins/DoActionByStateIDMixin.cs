using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByStateIDMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string[] stateIDs;
        [JsonProperty] public readonly BaseAction[] enterActions;
    }
}
