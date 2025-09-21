using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class ShowUICombatBar : BaseAction
{
    [JsonProperty] public string? barType { get; set; }
    [JsonProperty] public bool? show { get; set; }
    [JsonProperty] public float? duration { get; set; }
}