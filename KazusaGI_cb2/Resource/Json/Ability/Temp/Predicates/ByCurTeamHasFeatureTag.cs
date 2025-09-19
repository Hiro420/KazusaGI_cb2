using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByCurTeamHasFeatureTag : BasePredicate
{
    [JsonProperty] public readonly int featureTagID;
    [JsonProperty] public readonly LogicType logic;
}
