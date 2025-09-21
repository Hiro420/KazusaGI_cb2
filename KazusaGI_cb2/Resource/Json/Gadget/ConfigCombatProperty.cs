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
	[JsonProperty] float HP = 0f;
	[JsonProperty] bool isLockHP;
	[JsonProperty] bool isInvincible;
	[JsonProperty] bool isGhostToAllied;
	[JsonProperty] float attack = 0f;
	[JsonProperty] float defence = 0f;
	[JsonProperty] float weight = 0f;
	[JsonProperty] bool useCreatorProperty;
}
