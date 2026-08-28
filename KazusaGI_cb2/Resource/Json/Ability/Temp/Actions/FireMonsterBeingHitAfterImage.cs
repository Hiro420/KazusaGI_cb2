using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal class FireMonsterBeingHitAfterImage : BaseAction
	{
		[JsonProperty] public readonly TargetType target;
	}
}
