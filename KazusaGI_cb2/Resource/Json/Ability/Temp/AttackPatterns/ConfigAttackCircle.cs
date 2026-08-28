using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AttackPatterns
{
	internal class ConfigAttackCircle : BaseAttackPattern
	{
		[JsonProperty] public readonly object radius;
		[JsonProperty] public readonly TriggerType? triggerType;
		[JsonProperty] public readonly BaseBornType? born;
		[JsonProperty] public readonly float? secondHeight;
	}
}
