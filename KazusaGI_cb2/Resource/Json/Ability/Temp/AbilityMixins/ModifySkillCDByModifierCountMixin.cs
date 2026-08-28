using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
	internal class ModifySkillCDByModifierCountMixin : BaseAbilityMixin
	{
		[JsonProperty] public readonly TargetType targetType;
		[JsonProperty] public readonly string modifierName;
		[JsonProperty] public readonly int overtime;
		[JsonProperty] public readonly string cdDelta;
	}
}
