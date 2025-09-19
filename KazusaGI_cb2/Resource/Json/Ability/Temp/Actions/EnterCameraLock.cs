using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnterCameraLock : BaseAction
    {
        [JsonProperty] public readonly string transName;
        [JsonProperty] public readonly string cfgPath;
    }
}
