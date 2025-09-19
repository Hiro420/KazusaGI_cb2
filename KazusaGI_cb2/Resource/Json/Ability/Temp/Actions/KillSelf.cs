using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class KillSelf : BaseAction
    {
        [JsonProperty] public readonly BasePredicate[] predicates;
    }
}
