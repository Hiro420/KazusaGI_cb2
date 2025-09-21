using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

internal class SetAvatarCanShakeOff : BaseAction
{
    [JsonProperty] public bool? canShakeOff { get; set; }
    [JsonProperty] public float? shakeOffForce { get; set; }
    [JsonProperty] public string? shakeOffType { get; set; }
}