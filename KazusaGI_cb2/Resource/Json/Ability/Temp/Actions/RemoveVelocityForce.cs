using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal class RemoveVelocityForce : BaseAction
	{
		[JsonProperty] public readonly VelocityForceType[] forces;
	}
}
