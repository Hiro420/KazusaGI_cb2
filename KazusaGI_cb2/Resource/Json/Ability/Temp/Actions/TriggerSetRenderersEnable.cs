using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerSetRenderersEnable : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly bool canBeHandledOnRecover;
        [JsonProperty] public readonly string[] renderNames;
        [JsonProperty] public readonly bool setEnable;
    }
}
