using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KazusaGI_cb2.Resource.Json.Avatar;

public class ConfigCombatProperty
{
	[JsonProperty] public float HP = 0f;
	[JsonProperty] public bool isLockHP;
	[JsonProperty] public bool isInvincible;
	[JsonProperty] public bool isGhostToAllied;
	[JsonProperty] public float attack = 0f;
	[JsonProperty] public float defence = 0f;
	[JsonProperty] public float weight = 0f;
	[JsonProperty] public bool useCreatorProperty;
}
