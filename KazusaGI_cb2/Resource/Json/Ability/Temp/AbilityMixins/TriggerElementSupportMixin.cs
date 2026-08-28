using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
	internal class TriggerElementSupportMixin : BaseAbilityMixin
	{
		[JsonProperty] public readonly float duration;
		[JsonProperty] public readonly ElementType elementType;
	}
}
