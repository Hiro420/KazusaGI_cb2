using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class CalcDvalinS04RebornPoint : BaseAction
{
    [JsonProperty] public string? pointName { get; set; }
    [JsonProperty] public float? distance { get; set; }
    [JsonProperty] public bool? useAbsolute { get; set; }
}