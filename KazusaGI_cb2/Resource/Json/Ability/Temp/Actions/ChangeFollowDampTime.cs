using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ChangeFollowDampTime : BaseAction
    {
        [JsonProperty] public readonly string effectPattern;
        [JsonProperty] public readonly float PositionDampTime;
    }
}
