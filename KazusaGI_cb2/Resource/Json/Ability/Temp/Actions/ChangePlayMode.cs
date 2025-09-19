using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ChangePlayMode : BaseAction
    {
        [JsonProperty] public readonly bool? canBeHandledOnRecover;
        [JsonProperty] public readonly PlayMode? toPlayMode;
    }
}
