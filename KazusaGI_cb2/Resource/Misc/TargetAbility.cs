using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource;

public class TargetAbility
{
	[JsonProperty]
	public readonly string abilityID;
	[JsonProperty]
	public readonly string abilityName;
	[JsonProperty]
	public readonly string abilityOverride;
}