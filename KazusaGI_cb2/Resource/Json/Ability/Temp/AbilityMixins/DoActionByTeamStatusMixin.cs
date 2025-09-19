using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class DoActionByTeamStatusMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly BaseAction[] actions;
    }
}
