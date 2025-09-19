using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class Randomed : BaseAction
    {
        [JsonProperty] public readonly object chance;
        [JsonProperty] public readonly BaseAction[] successActions;
        [JsonProperty] public readonly BaseAction[] failActions;
    }
}
