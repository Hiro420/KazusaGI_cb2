using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class SetAISkillCDMultiplier : BaseAction
{
    [JsonProperty] public string? skillType { get; set; }
    [JsonProperty] public float? multiplier { get; set; }
    [JsonProperty] public float? duration { get; set; }
}