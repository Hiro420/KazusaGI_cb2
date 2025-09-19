using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;

public class SetAnimatorTrigger : BaseAction
{
    [JsonProperty] public readonly BasePredicate[]? predicates;
    [JsonProperty] public readonly string triggerID;
}
