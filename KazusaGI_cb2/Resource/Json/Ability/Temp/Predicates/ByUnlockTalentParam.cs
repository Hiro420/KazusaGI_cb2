using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByUnlockTalentParam : BasePredicate
{
    [JsonProperty] public readonly TargetType target;
    [JsonProperty] public readonly string talentParam;
}
