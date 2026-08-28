using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByTargetHPRatio : BasePredicate
{
	[JsonProperty] public readonly TargetType target;
	[JsonProperty] public readonly LogicType logic;
	[JsonProperty] public readonly object HPRatio;
}
