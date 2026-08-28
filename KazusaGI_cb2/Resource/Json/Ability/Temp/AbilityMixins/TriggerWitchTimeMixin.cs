using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
	internal class TriggerWitchTimeMixin : BaseAbilityMixin
	{
		[JsonProperty] public readonly TargetType ignoreTargetType;
		[JsonProperty] public readonly string weatherPattern;
	}
}
