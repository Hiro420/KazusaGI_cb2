using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ShowReminder : BaseAction
    {
        [JsonProperty] public readonly int reminderId;
        [JsonProperty] public readonly float duration;
        [JsonProperty] public readonly bool useRemotePlayerAvatar;
    }
}