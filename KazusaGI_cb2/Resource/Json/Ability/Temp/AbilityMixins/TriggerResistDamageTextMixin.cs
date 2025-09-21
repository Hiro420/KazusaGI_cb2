using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class TriggerResistDamageTextMixin : BaseAbilityMixin
{
    [JsonProperty] public string? textType { get; set; }
    [JsonProperty] public bool? showOnResist { get; set; }
    [JsonProperty] public float? displayDuration { get; set; }
}