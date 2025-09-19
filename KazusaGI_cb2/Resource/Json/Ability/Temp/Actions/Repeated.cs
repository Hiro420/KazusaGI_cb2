using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class Repeated : BaseAction
    {
        [JsonProperty] public readonly string repeatTimes;
        [JsonProperty] public readonly BaseAction[] actions;
    }
}
