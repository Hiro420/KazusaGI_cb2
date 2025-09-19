using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class StageReadyMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly BaseAction[] onStageReady;
    }
}
