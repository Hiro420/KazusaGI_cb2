using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Avatar;

public class ConfigAvatar
{
	[JsonProperty] public readonly List<TargetAbility> abilities;
}
