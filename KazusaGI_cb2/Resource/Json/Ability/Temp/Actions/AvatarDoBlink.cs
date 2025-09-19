using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AvatarDoBlink : BaseAction
    {
        [JsonProperty] public readonly bool PreferInput;
        [JsonProperty] public readonly float Distance;
    }
}
