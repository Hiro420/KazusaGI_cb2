using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class SetCanDieImmediately : BaseAction
{
    [JsonProperty] public bool? canDie { get; set; }
    [JsonProperty] public bool? skipDeathAnimation { get; set; }
    [JsonProperty] public string? deathType { get; set; }
}