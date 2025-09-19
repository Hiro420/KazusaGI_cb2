using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetCameraLockTime : BaseAction
    {
        [JsonProperty] public readonly float lockTime;
    }
}
