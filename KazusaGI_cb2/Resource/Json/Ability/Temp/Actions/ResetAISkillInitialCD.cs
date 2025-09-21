using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class ResetAISkillInitialCD : BaseAction
{
    [JsonProperty] public string? skillType { get; set; }
    [JsonProperty] public bool? resetAllSkills { get; set; }
    [JsonProperty] public float? delayTime { get; set; }
}