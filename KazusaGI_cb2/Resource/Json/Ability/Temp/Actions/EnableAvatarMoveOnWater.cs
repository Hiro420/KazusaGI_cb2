using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnableAvatarMoveOnWater : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly bool canBeHandledOnRecover;
        [JsonProperty] public readonly bool? enable;
    }
}
