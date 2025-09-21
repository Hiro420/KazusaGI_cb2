using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Json.Avatar;

public class ConfigGadget
{
	[JsonProperty] public readonly ConfigCombat? combat;
	[JsonProperty] public readonly List<TargetAbility> abilities;
}
