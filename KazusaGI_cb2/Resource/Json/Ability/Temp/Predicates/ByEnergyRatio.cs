using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
	internal class ByEnergyRatio : BasePredicate
	{
		[JsonProperty] public readonly object ratio;
		[JsonProperty] public readonly LogicType logic;
	}
}
