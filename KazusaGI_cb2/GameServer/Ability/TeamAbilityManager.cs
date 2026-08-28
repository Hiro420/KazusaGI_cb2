using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Level;

namespace KazusaGI_cb2.GameServer.Ability;

public class TeamAbilityManager : BaseAbilityManager
{
	private readonly TeamEntity _team;
	private bool _abilityCompInitFailed;

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => new();
	public override HashSet<string> ActiveDynamicAbilities => new();
	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => new();

	public TeamAbilityManager(TeamEntity owner) : base(owner)
	{
		_team = owner;
		InitAbilities();
	}

	private void InitAbilities()
	{
		// AbilityComp::addAllPreDynamicConfigAbilities is a no-op for entity type 9.
		if (!AddAllStaticConfigAbilities())
		{
			_abilityCompInitFailed = true;
			return;
		}

		if (!AddAllDynamicConfigAbilities())
			_abilityCompInitFailed = true;

		// addAllSkillDepotExtraAbilities is a no-op for non-avatar creatures.
		// Owner-avatar talent mixins run after these phases; their exact override
		// map pass is intentionally handled in the subsequent talent-runtime slice.
	}

	/// <summary>
	/// CB2 AbilityComp::addAllStaticConfigAbilities, entity type 9:
	/// ConfigGlobalCombat.defaultAbilities.defaultTeamAbilities in source order.
	/// </summary>
	private bool AddAllStaticConfigAbilities()
	{
		IEnumerable<string> abilities =
			MainApp.resourceManager.GlobalCombatData?.defaultAbilities?.defaultTeamAbilities
			?? [];

		foreach (string abilityName in abilities)
		{
			if (!TryResolveConfigAbility(abilityName, null, out ConfigAbility config) ||
				!TryAttachInitialAbility(config, null, "addAllStaticConfigAbilities.defaultTeamAbilities"))
			{
				return false;
			}
		}

		return true;
	}

	/// <summary>
	/// CB2 AbilityComp::addAllDynamicConfigAbilities, entity type 9:
	/// walk SceneEntity::findConfigLevelEntityPtrVec() in level-config-name order,
	/// then each ConfigLevelEntity.team_abilities vector in source order.
	/// </summary>
	private bool AddAllDynamicConfigAbilities()
	{
		Scene? scene = _team.session.player?.Scene;
		if (scene == null)
		{
			logger.LogWarning("addAllDynamicConfigAbilities(team): scene is null");
			return false;
		}

		foreach (ConfigLevelEntity levelConfig in scene.GetConfigLevelEntities())
		{
			foreach (TargetAbility entry in levelConfig.teamAbilities)
			{
				if (!TryResolveConfigAbility(entry.abilityName, entry.abilityOverride, out ConfigAbility config) ||
					!TryAttachInitialAbility(config, NormalizeOverride(entry.abilityOverride),
						"addAllDynamicConfigAbilities.team_abilities"))
				{
					return false;
				}
			}
		}

		return true;
	}

	private bool TryResolveConfigAbility(string? abilityName, string? abilityOverride, out ConfigAbility config)
	{
		if (!string.IsNullOrEmpty(abilityName) &&
			MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) &&
			container?.Default is ConfigAbility found)
		{
			config = found;
			return true;
		}

		logger.LogWarning($"findAbilityConfig failed for team entity {_team._EntityId}: ability={abilityName ?? "<null>"}, override={abilityOverride ?? "Default"}");
		config = null!;
		return false;
	}

	private bool TryAttachInitialAbility(ConfigAbility config, string? overrideName, string phase)
	{
		uint overrideHash = overrideName == null ? 0u : Utils.AbilityHash(overrideName);
		ActorAbility? actorAbility = AttachActorAbility(config, overrideName, 0, overrideHash);
		if (actorAbility == null)
		{
			logger.LogWarning($"{phase}: addNewAbility failed, ability={config.abilityName}, override={overrideName ?? "Default"}");
			return false;
		}

		// Keep legacy action execution aligned with the exact ActorAbility insertion
		// order; runtime identity remains AppliedAbilityMap.
		AddAbilityToEntity(_team, config);
		return true;
	}

	private static string? NormalizeOverride(string? abilityOverride)
	{
		return string.IsNullOrEmpty(abilityOverride) ||
			string.Equals(abilityOverride, "Default", StringComparison.Ordinal)
			? null
			: abilityOverride;
	}

	public override void Initialize()
	{
		// AbilityComp::init leaves is_init_finish_ false when any addAll* phase fails.
		if (_abilityCompInitFailed)
			return;

		base.Initialize();
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		await base.HandleAbilityInvokeAsync(invoke);
	}

	public override AbilitySyncStateInfo BuildAbilitySyncStateInfo()
	{
		// Client-init gating is migrated separately. Until then do not fabricate
		// is_inited/applied ability serialization here; runtime identity is already
		// authoritative for invoke handling.
		var syncInfo = new AbilitySyncStateInfo
		{
			IsInited = false
		};

		AppendAppliedModifiers(syncInfo);

		foreach (var kvp in AbilitySpecialOverrideMap)
		{
			foreach (var specialKvp in kvp.Value)
			{
				syncInfo.DynamicValueMaps.Add(new AbilityScalarValueEntry
				{
					Key = new AbilityString { Hash = specialKvp.Key },
					ValueType = AbilityScalarType.AbilityScalarTypeFloat,
					FloatValue = specialKvp.Value
				});
			}
		}

		foreach (var kvp in GlobalValueHashMap)
		{
			syncInfo.DynamicValueMaps.Add(new AbilityScalarValueEntry
			{
				Key = new AbilityString { Hash = kvp.Key },
				ValueType = AbilityScalarType.AbilityScalarTypeFloat,
				FloatValue = kvp.Value
			});
		}

		return syncInfo;
	}
}
