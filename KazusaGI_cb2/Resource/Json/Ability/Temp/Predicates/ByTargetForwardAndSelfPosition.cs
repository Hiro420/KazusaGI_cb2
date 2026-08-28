using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates;

public class ByTargetForwardAndSelfPosition : BasePredicate
{
	[JsonProperty] public readonly LogicType logic;
	[JsonProperty] public readonly float value;
}
