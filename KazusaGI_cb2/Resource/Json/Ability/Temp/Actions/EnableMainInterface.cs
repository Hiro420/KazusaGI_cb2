using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnableMainInterface : BaseAction
    {
        [JsonProperty] public readonly bool? doOffStage;
        [JsonProperty] public readonly bool? enable;
    }
}
