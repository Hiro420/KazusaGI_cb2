using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class DungeonFogEffects : BaseAction
    {
        [JsonProperty] public readonly bool muteRemoteAction;
        [JsonProperty] public readonly bool? enable;
        [JsonProperty] public readonly bool? doOffStage;
        [JsonProperty] public readonly string cameraFogEffectName;
        [JsonProperty] public readonly string playerFogEffectName;
    }
}