using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal class TurnDirection : BaseAction
	{
		[JsonProperty] public readonly string turnMode;
	}
}
