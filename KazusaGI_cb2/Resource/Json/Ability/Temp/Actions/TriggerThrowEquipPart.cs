using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class TriggerThrowEquipPart : BaseAction
{
    [JsonProperty] public string? partType { get; set; }
    [JsonProperty] public string? throwDirection { get; set; }
    [JsonProperty] public float? force { get; set; }
}