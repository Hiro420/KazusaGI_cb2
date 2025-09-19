using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnableAfterImage : BaseAction
    {
        [JsonProperty] public readonly bool? enable;
        [JsonProperty] public readonly bool? doOffStage;
    }
}
