using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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