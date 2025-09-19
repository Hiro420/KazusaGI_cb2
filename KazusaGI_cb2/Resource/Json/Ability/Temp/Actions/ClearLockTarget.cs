using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ClearLockTarget : BaseAction
    {
        [JsonProperty] public readonly bool onlyAvatar;
    }
}
