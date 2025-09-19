using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerHideWeapon : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly bool visible;
        [JsonProperty] public readonly string[] partNames;
    }
}
