using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class TriggerFaceAnimation : BaseAction
{
    [JsonProperty] public string? animationName { get; set; }
    [JsonProperty] public float? duration { get; set; }
    [JsonProperty] public bool? loop { get; set; }
}