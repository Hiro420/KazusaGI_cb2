using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Level;

/// <summary>
/// CB2 data::ConfigLevelEntity ability-bearing fields.
/// Other ConfigEntity members are intentionally left to their owning systems;
/// AbilityComp consumes these four vectors from the level entity config.
/// </summary>
public sealed class ConfigLevelEntity
{
	[JsonProperty] public readonly List<TargetAbility> abilities = new();
	[JsonProperty] public readonly List<TargetAbility> avatarAbilities = new();
	[JsonProperty] public readonly List<TargetAbility> teamAbilities = new();
	[JsonProperty] public readonly List<TargetAbility> monsterAbilities = new();
}
