using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnableHitBoxByName : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly string[] hitBoxNames;
        [JsonProperty] public readonly bool setEnable;
    }
}
