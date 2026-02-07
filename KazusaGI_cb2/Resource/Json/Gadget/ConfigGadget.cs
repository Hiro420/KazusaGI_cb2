using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
	[JsonProperty] public readonly JObject? move;
	
	public string? GetMoveTypeName()
	{
		return move?["$type"]?.ToString();
	}
	
	public bool HasNonHumanoidMove()
	{
		var moveType = GetMoveTypeName();
		if (string.IsNullOrEmpty(moveType))
			return false;
			
		// Check if move type is one of: ConfigSimpleMove, ConfigRigidBodyMove, ConfigAnimatorMove, ConfigMixinDriveMove
		return moveType.Contains("ConfigSimpleMove") ||
		       moveType.Contains("ConfigRigidBodyMove") ||
		       moveType.Contains("ConfigAnimatorMove") ||
		       moveType.Contains("ConfigMixinDriveMove");
	}
}
