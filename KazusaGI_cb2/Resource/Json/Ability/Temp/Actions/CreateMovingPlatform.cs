using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal class CreateMovingPlatform : BaseAction
	{
		[JsonProperty] public readonly bool lifeByOwnerIsAlive;
		[JsonProperty] public readonly BaseBornType born;
		[JsonProperty] public readonly int gadgetID;
		[JsonProperty] public readonly int campID;
		[JsonProperty] public readonly TargetType campTargetType;
		[JsonProperty] public readonly bool byServer;
	}
}
