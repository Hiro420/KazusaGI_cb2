using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal class ApplyModifier : BaseAction
	{
		[JsonProperty] public string? target { get; internal set; }
		[JsonProperty] public readonly bool doOffStage;
		[JsonProperty] public readonly BaseSelectTargetType? otherTargets;
		[JsonProperty] public string modifierName { get; internal set; } = string.Empty;
		[JsonProperty] public readonly BasePredicate[]? predicates;
	}
}
