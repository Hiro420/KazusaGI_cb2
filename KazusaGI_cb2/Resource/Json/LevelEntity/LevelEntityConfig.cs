using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Json;

public class LevelEntityConfig
{
	[JsonProperty] public readonly List<TargetAbility> abilities;
	[JsonProperty] public readonly List<TargetAbility> avatarAbilities;
	[JsonProperty] public readonly List<TargetAbility> teamAbilities;
	[JsonProperty] public readonly List<TargetAbility> monsterAbilities;
	// elemAmplifyDamage
}
