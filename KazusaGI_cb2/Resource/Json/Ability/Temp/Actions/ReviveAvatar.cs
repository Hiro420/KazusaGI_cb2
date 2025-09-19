using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ReviveAvatar : BaseAction
    {
        [JsonProperty] public readonly string target;
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public readonly string amountByTargetMaxHPRatio;
    }
}
