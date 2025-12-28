using Newtonsoft.Json;
using System.Collections.Generic;

namespace KazusaGI_cb2.Resource.Json.Monster;

public class ConfigMonster
{
	// We only model the fields needed for ability initialization.
	[JsonProperty] public readonly List<Resource.TargetAbility>? abilities;
}
