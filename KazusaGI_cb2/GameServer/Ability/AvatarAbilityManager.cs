using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Avatar;
using KazusaGI_cb2.Resource.Json.Level;
using KazusaGI_cb2.Resource.Json.Talent;

namespace KazusaGI_cb2.GameServer.Systems.Ability;

public class AvatarAbilityManager : BaseAbilityManager
{
	private readonly AvatarEntity _avatar;
	private SkillDepot CurDepot => _avatar.DbInfo.CurrentSkillDepot;
	private int CurDepotId => CurDepot.DepotId;
	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap => CurDepot.Abilities;
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => CurDepot.AbilitySpecials;
	public override HashSet<string> ActiveDynamicAbilities => CurDepot.ActiveDynamicAbilities;
	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => CurDepot.UnlockedTalentParams;

	public AvatarAbilityManager(AvatarEntity avatar) : base(avatar)
	{
		_avatar = avatar;
		InitAbilities();
	}

	private bool _abilityCompInitFailed;

	private void InitAbilities()
	{
		// AbilityComp::init clears its runtime identity maps before rebuilding them.
		// The manager is new here, but the SkillDepot compatibility dictionaries may
		// already contain the legacy pre-runtime projection, so rebuild those too.
		CurDepot.Abilities.Clear();
		CurDepot.AbilitySpecials.Clear();

		if (!AddAllStaticConfigAbilities())
		{
			_abilityCompInitFailed = true;
			return;
		}

		if (!AddAllDynamicConfigAbilities())
		{
			_abilityCompInitFailed = true;
			return;
		}

		if (!AddAllSkillDepotExtraAbilities())
			_abilityCompInitFailed = true;
	}

	/// <summary>
	/// CB2 AbilityComp::addAllStaticConfigAbilities avatar path.
	/// Avatar::initAbility has already copied ConfigAvatar::abilities into
	/// ability_entry_vec_; entries are validated in vector order and de-duplicated
	/// by abilityID + "_" + entityId before the completed static list is attached.
	/// </summary>
	private bool AddAllStaticConfigAbilities()
	{
		ResourceManager resourceManager = MainApp.resourceManager;
		string combatConfig = _avatar.DbInfo.serverAvatarExcel.CombatConfig;
		if (string.IsNullOrWhiteSpace(combatConfig) ||
			!resourceManager.ConfigAvatarMap.TryGetValue(combatConfig, out ConfigAvatar? configAvatar) ||
			configAvatar == null)
		{
			logger.LogWarning($"Avatar::initAbility getJsonConfig failed: avatar={_avatar.DbInfo.AvatarId}, combatConfig={combatConfig ?? "<null>"}");
			return false;
		}

		var staticAbilities = new List<(ConfigAbility Config, string? OverrideName)>();
		var abilityIdMap = new HashSet<string>(StringComparer.Ordinal);

		if (configAvatar.abilities != null)
		{
			foreach (TargetAbility entry in configAvatar.abilities)
			{
				// hk4e performs findAbilityConfig before ability_id_map_ lookup. A
				// missing config is logged and skipped during list construction.
				if (!TryResolveConfigAbility(entry.abilityName, entry.abilityOverride, out ConfigAbility? config))
					continue;

				string uniqueAbilityId = $"{entry.abilityID ?? string.Empty}_{_avatar._EntityId}";
				if (!abilityIdMap.Add(uniqueAbilityId))
					continue;

				staticAbilities.Add((config, NormalizeOverride(entry.abilityOverride)));
			}
		}

		foreach ((ConfigAbility config, string? overrideName) in staticAbilities)
		{
			if (!TryAttachInitialAbility(config, overrideName, "addAllStaticConfigAbilities"))
				return false;
		}

		return true;
	}

	/// <summary>
	/// CB2 AbilityComp::addAllDynamicConfigAbilities avatar branch: every
	/// ConfigLevelEntity.avatar_abilities entry first, then
	/// ConfigGlobalCombat.defaultAbilities.defaultAvatarAbilities.
	/// </summary>
	private bool AddAllDynamicConfigAbilities()
	{
		ResourceManager resourceManager = MainApp.resourceManager;
		Scene? scene = _avatar.session.player?.Scene;
		if (scene == null)
		{
			logger.LogWarning("addAllDynamicConfigAbilities(avatar): scene is null");
			return false;
		}

		foreach (ConfigLevelEntity levelConfig in scene.GetConfigLevelEntities())
		{
			foreach (TargetAbility entry in levelConfig.avatarAbilities)
			{
				if (!TryResolveConfigAbility(entry.abilityName, entry.abilityOverride, out ConfigAbility? config) ||
					!TryAttachInitialAbility(config, NormalizeOverride(entry.abilityOverride), "addAllDynamicConfigAbilities.avatar_abilities"))
					return false;
			}
		}

		List<string>? defaultAvatarAbilities = resourceManager.GlobalCombatData?.defaultAbilities?.defaultAvatarAbilities;
		if (defaultAvatarAbilities != null)
		{
			foreach (string abilityName in defaultAvatarAbilities)
			{
				if (!TryResolveConfigAbility(abilityName, null, out ConfigAbility? config) ||
					!TryAttachInitialAbility(config, null, "addAllDynamicConfigAbilities.defaultAvatarAbilities"))
					return false;
			}
		}

		return true;
	}

	/// <summary>
	/// CB2 AbilityComp::addAllSkillDepotExtraAbilities avatar branch.
	/// Empty strings are skipped; any non-empty add failure aborts the phase.
	/// </summary>
	private bool AddAllSkillDepotExtraAbilities()
	{
		ResourceManager resourceManager = MainApp.resourceManager;
		uint skillDepotId = _avatar.DbInfo.SkillDepotId;
		if (!resourceManager.AvatarSkillDepotExcel.TryGetValue(skillDepotId, out AvatarSkillDepotExcelConfig? depot) || depot == null)
		{
			logger.LogWarning($"findAvatarSkillDepotExcelConfig failed, skill_depot_id={skillDepotId}");
			return false;
		}

		if (depot.extraAbilities == null)
			return true;

		foreach (string abilityName in depot.extraAbilities)
		{
			if (string.IsNullOrEmpty(abilityName))
				continue;

			if (!TryResolveConfigAbility(abilityName, null, out ConfigAbility? config) ||
				!TryAttachInitialAbility(config, null, "addAllSkillDepotExtraAbilities"))
				return false;
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

		logger.LogWarning($"findAbilityConfig failed for avatar {_avatar.DbInfo.AvatarId}: ability={abilityName ?? "<null>"}, override={abilityOverride ?? "Default"}");
		config = null!;
		return false;
	}

	private bool TryAttachInitialAbility(ConfigAbility config, string? overrideName, string phase)
	{
		uint overrideHash = overrideName == null ? 0u : KazusaGI_cb2.GameServer.Ability.Utils.AbilityHash(overrideName);
		ActorAbility? actorAbility = AttachActorAbility(config, overrideName, 0, overrideHash);
		if (actorAbility == null)
		{
			logger.LogWarning($"{phase}: addNewAbility failed, ability={config.abilityName}, override={overrideName ?? "Default"}");
			return false;
		}

		if (!CurDepot.AbilitySpecials.ContainsKey(config.abilityName))
			CurDepot.AbilitySpecials[config.abilityName] = BuildAbilitySpecials(config);

		// Compatibility execution object. It must follow the same insertion order as
		// applied_ability_map_ so current action dispatch does not invent a second order.
		AddAbilityToEntity(_avatar, config);
		return true;
	}

	private static string? NormalizeOverride(string? abilityOverride)
	{
		return string.IsNullOrEmpty(abilityOverride) ||
			string.Equals(abilityOverride, "Default", StringComparison.Ordinal)
			? null
			: abilityOverride;
	}

	private static Dictionary<string, float> BuildAbilitySpecials(ConfigAbility config)
	{
		var specials = new Dictionary<string, float>();
		if (config.abilitySpecials == null)
			return specials;
		foreach (var kvp in config.abilitySpecials)
		{
			if (kvp.Value.TryGetSingle(out var value))
				specials[kvp.Key] = value;
		}
		return specials;
	}

	private static bool TryReadSpecialValue(object? valueObj, out float value)
	{
		switch (valueObj)
		{
			case null:
				value = 0f;
				return false;
			case float floatValue:
				value = floatValue;
				return true;
			case int intValue:
				value = intValue;
				return true;
			case long longValue:
				value = longValue;
				return true;
			case double doubleValue:
				value = (float)doubleValue;
				return true;
			case decimal decimalValue:
				value = (float)decimalValue;
				return true;
			case string stringValue:
				return float.TryParse(stringValue, out value);
			default:
				value = 0f;
				return false;
		}
	}

	public override void Initialize()
	{
		if (_abilityCompInitFailed)
			return;

		var avatar = Owner as AvatarEntity;
		if (avatar?.DbInfo == null)
		{
			base.Initialize();
			return;
		}

		var db = avatar.DbInfo;

		if (CurDepot?.InherentProudSkillOpens != null)
		{
			foreach (var proudSkillOpen in CurDepot.InherentProudSkillOpens)
			{
				var openConfig = proudSkillOpen.openConfig;
				if (string.IsNullOrWhiteSpace(openConfig))
					continue;
				if (!db.ConfigTalentMap.TryGetValue(CurDepotId, out var depotTalentMap))
					continue;
				if (!depotTalentMap.TryGetValue(openConfig, out var talents) || talents == null)
					continue;
				foreach (BaseConfigTalent config in talents)
					config?.Apply(this, proudSkillOpen.paramList);
			}
		}

		ProudSkillExcelConfig? FindProudSkill(uint depotId, uint proudSkillGroupId, int level)
		{
			if (!db.ProudSkillData.TryGetValue((int)depotId, out var proudMap) || proudMap == null)
				return null;
			return proudMap.Values.FirstOrDefault(p =>
				p != null &&
				p.proudSkillGroupId == proudSkillGroupId &&
				p.level == level);
		}

		void ApplyTalentsFromDepot(int depotId, string? openConfig, IList<double>? paramList)
		{
			if (string.IsNullOrWhiteSpace(openConfig))
				return;
			if (!db.ConfigTalentMap.TryGetValue(depotId, out var depotTalentMap) || depotTalentMap == null)
				return;
			if (!depotTalentMap.TryGetValue(openConfig, out var talents) || talents == null)
				return;
			foreach (var config in talents)
				config?.Apply(this, paramList?.ToArray() ?? Array.Empty<double>());
		}

		if (CurDepot?.Skills != null)
		{
			foreach (var skill in CurDepot.Skills)
			{
				if (!db.SkillData.TryGetValue(CurDepotId, out var skillMap) || skillMap == null)
					continue;
				var skillId = (uint)skill.Key;
				if (!skillMap.TryGetValue(skillId, out var skillData) || skillData == null)
					continue;
				var proudSkill = FindProudSkill((uint)CurDepotId, skillData.proudSkillGroupId, skill.Value);
				if (proudSkill == null)
					continue;
				ApplyTalentsFromDepot(CurDepotId, proudSkill.openConfig, proudSkill.paramList);
			}
		}

		if (CurDepot?.Element != null)
		{
			uint energySkillId = (uint)CurDepot.EnergySkill;
			int energySkillLevel = CurDepot.EnergySkillLevel;
			if (db.SkillData.TryGetValue(CurDepotId, out var skillMap) &&
				skillMap != null &&
				skillMap.TryGetValue(energySkillId, out var energySkillData) &&
				energySkillData != null)
			{
				var proudSkill = FindProudSkill((uint)CurDepotId, energySkillData.proudSkillGroupId, energySkillLevel);
				if (proudSkill != null && !string.IsNullOrWhiteSpace(proudSkill.openConfig))
				{
					foreach (var depotTalentMap in db.ConfigTalentMap.Values)
					{
						if (depotTalentMap == null) continue;
						if (!depotTalentMap.TryGetValue(proudSkill.openConfig, out var talents) || talents == null) continue;
						foreach (var config in talents)
							config?.Apply(this, proudSkill.paramList);
					}
				}
			}
		}

		base.Initialize();
	}

	protected override void AddAbility(AbilityAppliedAbility ability)
	{
		base.AddAbility(ability);
	}

	public override Protocol.AbilitySyncStateInfo BuildAbilitySyncStateInfo()
	{
		var syncInfo = new Protocol.AbilitySyncStateInfo
		{
			IsInited = false  // Avatars send applied_abilities via AbilityMetaAddAbility, not from config
		};

		AppendAppliedModifiers(syncInfo);

		// DynamicValueMaps: from special overrides and global values
		var abilitySpecialOverrideMap = AbilitySpecialOverrideMap;
		var globalValueHashMap = GlobalValueHashMap;

		if (abilitySpecialOverrideMap.Count > 0)
		{
			foreach (var kvp in abilitySpecialOverrideMap)
			{
				foreach (var specialKvp in kvp.Value)
				{
					var entry = new Protocol.AbilityScalarValueEntry
					{
						Key = new Protocol.AbilityString { Hash = specialKvp.Key },
						ValueType = AbilityScalarType.AbilityScalarTypeFloat,
						FloatValue = specialKvp.Value
					};
					syncInfo.DynamicValueMaps.Add(entry);
				}
			}
		}

		if (globalValueHashMap.Count > 0)
		{
			foreach (var kvp in globalValueHashMap)
			{
				var entry = new Protocol.AbilityScalarValueEntry
				{
					Key = new Protocol.AbilityString { Hash = kvp.Key },
					ValueType = AbilityScalarType.AbilityScalarTypeFloat,
					FloatValue = kvp.Value
				};
				syncInfo.DynamicValueMaps.Add(entry);
			}
		}

		return syncInfo;
	}
}