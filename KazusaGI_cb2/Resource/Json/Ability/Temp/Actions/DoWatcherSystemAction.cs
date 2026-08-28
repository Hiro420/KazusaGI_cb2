using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
	internal class DoWatcherSystemAction : BaseAction
	{
		[JsonProperty] public readonly TargetType target;
		[JsonProperty] public readonly int watcherId;
		[JsonProperty] public readonly bool authorityOnly;
	}
}
