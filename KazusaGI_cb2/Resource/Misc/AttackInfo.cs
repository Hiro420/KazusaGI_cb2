using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource;

public class AttackInfo
{
	[JsonProperty] public readonly string attackTag;
	[JsonProperty] public readonly string attenuationTag;
	[JsonProperty] public readonly string attenuationGroup;
	[JsonProperty] public readonly AttackProperty attackProperty;
	[JsonProperty] public readonly HitPattern hitPattern;
	[JsonProperty] public readonly TargetType? canBeModifiedBy;

	public class AttackProperty
	{
		[JsonProperty] public readonly object? bonusCriticalHurt;
		[JsonProperty] public readonly object damagePercentageRatio;
		[JsonProperty] public readonly ElementType elementType;
		[JsonProperty] public readonly object elementDurability;
	}

	public class HitPattern
	{
		[JsonProperty] public readonly string onHitEffectName;
		[JsonProperty] public readonly float hitImpulseX;
		[JsonProperty] public readonly float hitImpulseY;
		[JsonProperty] public readonly string hitImpulseType;
		[JsonProperty] public readonly float hitHaltTimeScale;
	}
}