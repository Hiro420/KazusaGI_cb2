using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace KazusaGI_cb2.Resource.Json.Monster;

public class ConfigMonster
{
	// We only model the fields needed for ability initialization.
	[JsonProperty] public readonly List<Resource.TargetAbility>? abilities;
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
