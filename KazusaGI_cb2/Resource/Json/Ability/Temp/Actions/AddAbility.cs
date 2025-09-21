using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class AddAbilityAction : BaseAction
{
    [JsonProperty] public string? abilityName;
    [JsonProperty] public string? abilityOverride;
}