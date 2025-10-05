using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Json;

public class ConfigMonster
{
	[JsonProperty] public readonly List<TargetAbility> abilities;
}
