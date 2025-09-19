using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class Predicated : BaseAction
    {
        [JsonProperty] public readonly BasePredicate[] targetPredicates;
        [JsonProperty] public readonly BaseAction[] successActions;
        [JsonProperty] public readonly BaseAction[] failActions;
    }
}
