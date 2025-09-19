using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerSetPassThrough : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly bool canBeHandledOnRecover;
        [JsonProperty] public readonly bool passThrough;
    }
}
