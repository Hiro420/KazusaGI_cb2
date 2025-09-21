using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class UseSkillEliteSet : BaseAction
{
    [JsonProperty] public string? skillType { get; set; }
    [JsonProperty] public string? eliteSetKey { get; set; }
    [JsonProperty] public bool? forceUse { get; set; }
}