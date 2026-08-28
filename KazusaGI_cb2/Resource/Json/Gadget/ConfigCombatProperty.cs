using Newtonsoft.Json;

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
