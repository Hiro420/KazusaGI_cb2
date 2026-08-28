using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Avatar;
using KazusaGI_cb2.Resource.Json.Talent;
using KazusaGI_cb2.Resource.ServerExcel;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

public class PlayerAvatar
{
	private static ResourceManager resourceManager = MainApp.resourceManager;
	private Session Session { get; set; }
	public AvatarExcelConfig avatarExcel { get; set; }
	public AvatarRow serverAvatarExcel { get; set; }
	public AvatarSkillDepotExcelConfig avatarSkillDepotExcel { get; set; }
	public ulong Guid { get; set; } // persistent hk4e guid_
	public uint AvatarId { get; set; } // persistent avatar_id_

	// Runtime-only Entity state. hk4e keeps these directly on Avatar and clears
	// them in Scene::delAvatarAndWeaponEntity; they are never persisted.
	public uint EntityId { get; internal set; }
	public uint LastMoveSceneTimeMs { get; internal set; }
	public uint LastMoveReliableSeq { get; internal set; }
	public List<Protocol.Vector> LastMoveParams { get; } = new();

	// Avatar component state which is emitted by BuffComp/SkillComp/TalentComp.
	public HashSet<uint> BuffIds { get; } = new();
	public Dictionary<uint, AvatarSkillRuntimeState> SkillStates { get; } = new();
	public Dictionary<uint, uint> ProudSkillExtraLevels { get; } = new();
	public uint FetterExpNumber { get; set; }
	public uint FetterLevel { get; set; } = 1;
	public HashSet<uint> FetterOpenIds { get; } = new();

	public uint Level { get; set; }
	public uint Exp { get; set; }
	public float Hp { get; set; }
	public float MaxHp { get; set; }
	public float Def { get; set; }
	public float Atk { get; set; }
	public float CritRate { get; set; }
	public float CritDmg { get; set; }
	public float EM { get; set; }
	public uint PromoteLevel { get; set; }
	public uint BreakLevel { get; set; }
	public float CurElemEnergy { get; set; }
	public uint SkillDepotId { get; set; }
	public uint UltSkillId { get; set; }
	public ulong EquipGuid { get; set; }
	public Dictionary<uint, uint> SkillLevels { get; set; }
	public HashSet<uint> UnlockedTalents { get; set; }
	public HashSet<uint> ProudSkills { get; set; }
	public string AvatarName => avatarExcel.iconName.Split("_").Last();

	public readonly SortedList<int, SortedList<uint, AvatarSkillExcelConfig>> SkillData = new();
	public readonly SortedList<int, SortedList<uint, AvatarTalentExcelConfig>> TalentData = new();
	public readonly SortedList<int, SortedList<uint, ProudSkillExcelConfig>> ProudSkillData = new();
	public readonly SortedList<int, Dictionary<uint, ConfigAbility>?> AbilityHashMap = new();
	public readonly SortedList<int, ConfigAbilityContainer[]> AbilityConfigMap = new(); // depotId
	public readonly SortedList<int, Dictionary<string, BaseConfigTalent[]>> ConfigTalentMap = new(); // <depotId, file name>
	public SkillDepot CurrentSkillDepot { get; private set; }

	public PlayerAvatar(Session session, uint AvatarId, ulong? overrideGuid = null)
	{
		this.Session = session;
		this.avatarExcel = resourceManager.AvatarExcel[AvatarId];
		this.serverAvatarExcel = resourceManager.ServerAvatarRows.First(av => av.Id == AvatarId)!;
		this.SkillDepotId = this.isTraveler() ? avatarExcel.candSkillDepotIds[3] : avatarExcel.skillDepotId;
		this.avatarSkillDepotExcel = resourceManager.AvatarSkillDepotExcel[this.SkillDepotId];
		this.UltSkillId = avatarSkillDepotExcel.energySkill;
		this.SkillLevels = new Dictionary<uint, uint>();
		this.UnlockedTalents = new HashSet<uint>();
		this.ProudSkills = new HashSet<uint>();
		this.Guid = overrideGuid ?? MainApp.GuidMgr.GenGuid(GuidMgr.GuidType.Avatar);
		this.AvatarId = AvatarId;
		this.Level = 50;
		this.Exp = 0;
		this.BreakLevel = 3;
		this.Hp = avatarExcel.hpBase;
		this.MaxHp = avatarExcel.hpBase;
		this.Def = avatarExcel.defenseBase;
		this.Atk = avatarExcel.attackBase;
		this.CritRate = avatarExcel.critical;
		this.CritDmg = avatarExcel.criticalHurt;
		this.EM = 0;
		this.PromoteLevel = 3;
		this.CurElemEnergy = 1000;
		uint initialWeapon = avatarExcel.initialWeapon;
		if (initialWeapon != 0)
		{
			PlayerWeapon weapon = new(session, initialWeapon);
			weapon.EquipOnAvatar(this, false);
		}
		this.SkillLevels.Add(this.UltSkillId, 1); // todo: get from resources
		foreach (uint skillId in avatarSkillDepotExcel.skills.Concat(avatarSkillDepotExcel.subSkills ?? new List<uint>()))
		{
			if (skillId == 0) continue;
			this.SkillLevels.TryAdd(skillId, 1);
		}
		foreach (uint talentId in avatarSkillDepotExcel.talents)
		{
			if (talentId == 0) continue;
			this.UnlockedTalents.Add(talentId);
		}
		foreach (ProudSkillOpenConfig proudSkillOpenConfig in this.avatarSkillDepotExcel.inherentProudSkillOpens)
		{
			if (this.PromoteLevel < proudSkillOpenConfig.needAvatarPromoteLevel)
				continue;
			uint proudSkillGroupId = proudSkillOpenConfig.proudSkillGroupId;
			ProudSkillExcelConfig? proudSkillExcel = resourceManager.ProudSkillExcel
				.Select(kv => kv.Value) // Flatten the nested dictionaries
				.FirstOrDefault(config => config.proudSkillGroupId == proudSkillGroupId);

			if (proudSkillExcel != null)
			{
				this.ProudSkills.Add(proudSkillExcel.proudSkillId);
			}
		}
		foreach (AvatarSkillDepotExcelConfig depot in resourceManager.AvatarSkillDepotExcel.Values)
		{
			if (!resourceManager.ConfigAvatarMap.TryGetValue(serverAvatarExcel.CombatConfig, out ConfigAvatar? configAvatar))
			{
				session.c.LogWarning($"ConfigAvatar not found for AvatarId {AvatarId} with CombatConfig {serverAvatarExcel.CombatConfig}");
				continue;
			}
			var configContainers = new List<ConfigAbilityContainer>();
			configAvatar.abilities.ForEach(t => configContainers.Add(resourceManager.ConfigAbilityMap[t.abilityName]));
			AbilityConfigMap.Add((int)depot.id, configContainers.ToArray());
			var dictionary1 = resourceManager.AvatarSkillExcel.Where(w => depot.skills.Contains(w.Key) || depot.subSkills.Contains(w.Key) || depot.energySkill == w.Key).ToDictionary(x => x.Key, x => x.Value);
			SkillData.Add((int)depot.id, new SortedList<uint, AvatarSkillExcelConfig>(dictionary1));
			var dictionary7 = resourceManager.AvatarTalentExcel.Where(w => depot.talents.Contains(w.Value.talentId)).ToDictionary(x => x.Key, x => x.Value);
			TalentData.Add((int)depot.id, new SortedList<uint, AvatarTalentExcelConfig>(dictionary7));
			var dictionary8 = resourceManager.ProudSkillExcel.Where(w => depot.inherentProudSkillOpens.Exists(y => y.proudSkillGroupId == w.Value.proudSkillGroupId)).ToDictionary(x => x.Key, x => x.Value);
			ProudSkillData.Add((int)depot.id, new SortedList<uint, ProudSkillExcelConfig>(dictionary8));
			foreach (var skilldata in dictionary1.Values)
			{
				var proudData = resourceManager.ProudSkillExcel.Where(w => w.Value.proudSkillGroupId == skilldata.proudSkillGroupId);
				foreach (var proud in proudData)
				{
					ProudSkillData[(int)depot.id][proud.Key] = proud.Value;
				}
			}
			foreach (var talent in dictionary7.Values)
			{
				var configTalents = resourceManager.AvatarTalentConfigDataMap
					.Where(kv => kv.Key == talent.openConfig);
				ConfigTalentMap[(int)depot.id] = configTalents.ToDictionary();
			}
			Dictionary<uint, ConfigAbility> abilityHashMap = new();
			foreach (TargetAbility ability in configAvatar.abilities)
			{
				ConfigAbility? config = null;
				foreach (var container in AbilityConfigMap[(int)depot.id])
				{
					if (container.Default is ConfigAbility konfig && konfig.abilityName == ability.abilityName)
					{
						config = konfig;
						break;
					}
				}
				if (config == null) continue;
				abilityHashMap[(uint)Ability.Utils.AbilityHash(ability.abilityName)] = config;
			}
			AbilityHashMap.Add((int)depot.id, abilityHashMap);
		}

		// Initialize skill depot
		CurrentSkillDepot = new SkillDepot(this, (int)this.SkillDepotId);


		ReCalculateFightProps();
	}

	/// <summary>Avatar::toClient(SceneAvatarInfo) + Avatar component toClient calls.</summary>
	public SceneAvatarInfo ToSceneAvatarInfo()
	{
		var sceneAvatarInfo = new SceneAvatarInfo
		{
			PeerId = Session.player!.PeerId,
			Guid = Guid,
			AvatarId = AvatarId,
			Uid = Session.player.Uid,
			SkillDepotId = SkillDepotId,
			CoreProudSkillLevel = GetCoreProudSkillLevel(),
		};

		// EquipComp::toClient(SceneAvatarInfo).
		if (EquipGuid != 0 && Session.player.weaponDict.TryGetValue(EquipGuid, out var weapon))
		{
			sceneAvatarInfo.EquipIdLists.Add(weapon.WeaponId);
			sceneAvatarInfo.Weapon = weapon.ToSceneWeaponInfo(Session);
		}

		// TalentComp::toClient(SceneAvatarInfo).
		sceneAvatarInfo.TalentIdLists.AddRange(UnlockedTalents.OrderBy(x => x));
		sceneAvatarInfo.InherentProudSkillLists.AddRange(ProudSkills.OrderBy(x => x));
		foreach (var (groupId, extraLevel) in ProudSkillExtraLevels)
			sceneAvatarInfo.ProudSkillExtraLevelMaps[groupId] = extraLevel;

		// SkillComp::toClient(SceneAvatarInfo): only the current depot's level map.
		foreach (var (skillId, level) in SkillLevels)
			sceneAvatarInfo.SkillLevelMaps[skillId] = level;

		// BuffComp::toClient(SceneAvatarInfo).
		sceneAvatarInfo.BuffIdLists.AddRange(BuffIds.OrderBy(x => x));
		return sceneAvatarInfo;
	}

	/// <summary>Avatar::toClient(AvatarEnterSceneInfo).</summary>
	public AvatarEnterSceneInfo ToAvatarEnterSceneInfo(Scene scene)
	{
		AvatarEntity avatarEntity = scene.AddAvatarAndWeaponEntity(this, isEnterScene: true);
		var info = new AvatarEnterSceneInfo
		{
			AvatarGuid = Guid,
			AvatarEntityId = avatarEntity._EntityId,
			AvatarAbilityInfo = avatarEntity.BuildAbilityInfo()
		};
		info.BuffIdLists.AddRange(BuffIds.OrderBy(x => x));

		if (EquipGuid != 0 && Session.player!.weaponDict.TryGetValue(EquipGuid, out var weapon))
		{
			WeaponEntity weaponEntity = scene.GetOrCreateWeaponEntity(weapon);
			info.WeaponGuid = weapon.Guid;
			info.WeaponEntityId = weaponEntity._EntityId;
			info.WeaponAbilityInfo = weaponEntity.BuildAbilityInfo();
		}
		return info;
	}

	public uint GetCoreProudSkillLevel()
	{
		uint groupId = avatarSkillDepotExcel.coreProudSkillGroupId;
		if (groupId == 0) return 0;
		uint level = ProudSkills
			.Select(id => resourceManager.ProudSkillExcel.Values.FirstOrDefault(p => p.proudSkillId == id))
			.Where(p => p != null && p.proudSkillGroupId == groupId)
			.Select(p => p!.level)
			.DefaultIfEmpty(0u)
			.Max();
		// TalentComp::unlockDefaultProudSkill creates level 1 once the avatar
		// reaches the depot's core-proud promote threshold.
		if (level == 0 && PromoteLevel >= avatarSkillDepotExcel.coreProudAvatarPromoteLevel)
			level = 1;
		return level;
	}

	public void RestoreSkillDepot(uint skillDepotId, uint ultSkillId = 0)
	{
		if (skillDepotId == 0 || !resourceManager.AvatarSkillDepotExcel.TryGetValue(skillDepotId, out var depot))
			return;

		SkillDepotId = skillDepotId;
		avatarSkillDepotExcel = depot;
		UltSkillId = ultSkillId != 0 ? ultSkillId : depot.energySkill;
		CurrentSkillDepot = new SkillDepot(this, (int)SkillDepotId);
	}

	public bool isTraveler()
	{
		return this.avatarExcel.candSkillDepotIds.Count > 0;
	}

	/// <summary>Avatar::toClient(AvatarInfo) + FormalAvatar component serialization.</summary>
	public AvatarInfo ToAvatarInfo()
	{
		var avatarInfo = new AvatarInfo
		{
			Guid = Guid,
			AvatarId = AvatarId,
			LifeState = Hp > 0 ? 1u : 2u,
			SkillDepotId = SkillDepotId,
			AvatarType = (uint)AvatarType.AvatarTypeFormal,
			CoreProudSkillLevel = GetCoreProudSkillLevel(),
			FetterInfo = new AvatarFetterInfo
			{
				ExpNumber = FetterExpNumber,
				ExpLevel = FetterLevel
			}
		};
		avatarInfo.FetterInfo.OpenIdLists.AddRange(FetterOpenIds.OrderBy(x => x));

		// Creature/Avatar property state.
		AddPropMap(PropType.PROP_LEVEL, Level, avatarInfo.PropMaps);
		AddPropMap(PropType.PROP_EXP, Exp, avatarInfo.PropMaps);
		AddPropMap(PropType.PROP_BREAK_LEVEL, BreakLevel, avatarInfo.PropMaps);

		foreach (var (prop, value) in BuildFightPropMap())
			avatarInfo.FightPropMaps[prop] = value;

		// EquipComp::toClient(AvatarInfo): GUIDs, not item ids.
		if (EquipGuid != 0)
			avatarInfo.EquipGuidLists.Add(EquipGuid);

		// TalentComp::toClient(AvatarInfo).
		avatarInfo.TalentIdLists.AddRange(UnlockedTalents.OrderBy(x => x));
		avatarInfo.InherentProudSkillLists.AddRange(ProudSkills.OrderBy(x => x));
		foreach (var (groupId, extraLevel) in ProudSkillExtraLevels)
			avatarInfo.ProudSkillExtraLevelMaps[groupId] = extraLevel;

		// SkillComp::toClient(AvatarInfo): full Skill::toClient state plus levels.
		foreach (var (skillId, level) in SkillLevels)
		{
			avatarInfo.SkillLevelMaps[skillId] = level;
			SkillStates.TryGetValue(skillId, out var state);
			resourceManager.AvatarSkillExcel.TryGetValue(skillId, out var skillConfig);
			var skillInfo = new AvatarSkillInfo
			{
				PassCdTime = state?.PassCdTime ?? 0,
				MaxChargeCount = state?.MaxChargeCount ?? (uint)Math.Max(skillConfig?.maxChargeNum ?? 1, 1)
			};
			if (state != null)
				skillInfo.FullCdTimeLists.AddRange(state.FullCdTimes);
			avatarInfo.SkillMaps[skillId] = skillInfo;
		}
		return avatarInfo;
	}

	/// <summary>
	/// Fill the runtime avatar entity snapshot directly from authoritative
	/// avatar state. This avoids round-tripping through AvatarInfo just to
	/// recover prop/fight-prop maps.
	/// </summary>
	public void FillSceneEntityState(SceneEntityInfo info)
	{
		AddPropMap(PropType.PROP_LEVEL, Level, info.PropMaps);
		AddPropMap(PropType.PROP_EXP, Exp, info.PropMaps);
		AddPropMap(PropType.PROP_BREAK_LEVEL, BreakLevel, info.PropMaps);
		ReCalculateFightProps();
		AddAllFightProps(info.FightPropMaps);
	}

	// todo: reliquary
	/// <summary>
	/// Creature/FightPropComp calculation for the subset of CB2 data currently
	/// loaded by this server. Values are kept in hk4e's native units: percent
	/// fight props are fractions, not multiplied by 100.
	/// </summary>
	public Dictionary<uint, float> BuildFightPropMap()
	{
		var props = new Dictionary<uint, float>();
		void Set(FightPropType type, float value) => props[(uint)type] = value;
		void Add(FightPropType type, float value) => props[(uint)type] = props.GetValueOrDefault((uint)type) + value;

		float oldMaxHp = MaxHp;
		float oldHp = Hp;
		bool wasFullHp = oldMaxHp > 0f && oldHp >= oldMaxHp - 0.001f;

		AvatarCurveExcelConfig? avatarCurve = resourceManager.AvatarCurveExcel.GetValueOrDefault(Math.Max(Level, 1));
		foreach (FightPropGrowConfig grow in avatarExcel.propGrowCurves ?? new List<FightPropGrowConfig>())
		{
			float initial = grow.type switch
			{
				FightPropType.FIGHT_PROP_BASE_HP => avatarExcel.hpBase,
				FightPropType.FIGHT_PROP_BASE_ATTACK => avatarExcel.attackBase,
				FightPropType.FIGHT_PROP_BASE_DEFENSE => avatarExcel.defenseBase,
				_ => 0f
			};
			GrowCurveInfo? curve = avatarCurve?.curveInfos?.FirstOrDefault(x => x.type == grow.growCurve);
			Set(grow.type, curve == null ? initial : ApplyCurve(initial, curve));
		}

		// Excel base props not represented in propGrowCurves still exist.
		props.TryAdd((uint)FightPropType.FIGHT_PROP_BASE_HP, avatarExcel.hpBase);
		props.TryAdd((uint)FightPropType.FIGHT_PROP_BASE_ATTACK, avatarExcel.attackBase);
		props.TryAdd((uint)FightPropType.FIGHT_PROP_BASE_DEFENSE, avatarExcel.defenseBase);
		Set(FightPropType.FIGHT_PROP_CRITICAL, avatarExcel.critical);
		Set(FightPropType.FIGHT_PROP_CRITICAL_HURT, avatarExcel.criticalHurt);
		Set(FightPropType.FIGHT_PROP_CHARGE_EFFICIENCY,
			avatarExcel.chargeEfficiency != 0f ? avatarExcel.chargeEfficiency : 1f);

		if (EquipGuid != 0 && Session.player!.weaponDict.TryGetValue(EquipGuid, out var weapon) &&
			resourceManager.WeaponExcel.TryGetValue(weapon.WeaponId, out var weaponExcel))
		{
			WeaponCurveExcelConfig? weaponCurve = resourceManager.WeaponCurveExcel.GetValueOrDefault(Math.Max(weapon.Level, 1));
			foreach (WeaponProperty property in weaponExcel.weaponProp ?? new List<WeaponProperty>())
			{
				if (property.propType == FightPropType.FIGHT_PROP_NONE) continue;
				float value = property.initValue;
				GrowCurveInfo? curve = weaponCurve?.curveInfos?.FirstOrDefault(x => x.type == property.type);
				if (curve != null)
					value = ApplyCurve(value, curve);
				Add(property.propType, value);
			}

			// Weapon::assignPromoteProp adds the current promote row's values,
			// but only for prop types declared by WeaponExcelConfig::weapon_prop.
			if (resourceManager.WeaponPromoteExcel.TryGetValue(weaponExcel.weaponPromoteId, out var promotes))
			{
				WeaponPromoteExcelConfig? promote = promotes.GetValueOrDefault(weapon.PromoteLevel)
					?? promotes.Values.FirstOrDefault(x => x.promoteLevel == weapon.PromoteLevel);
				if (promote != null)
				{
					foreach (AddProp addProp in promote.addProps ?? new List<AddProp>())
					{
						if (!TryParseFightProp(addProp.propType, out var type) ||
							!(weaponExcel.weaponProp?.Any(x => x.propType == type) ?? false))
							continue;
						Add(type, (float)(addProp.value ?? 0d));
					}
				}
			}

			// EquipAffix::getAffixProp cumulatively sums rows from level 0 up
			// through the stored refinement level for every weapon affix id.
			foreach (var (affixId, affixLevel) in GetWeaponAffixMap(weapon))
			{
				foreach (var affix in resourceManager.EquipAffixExcel.Values
							 .Where(x => x.AffixId == affixId && x.Level <= affixLevel)
							 .OrderBy(x => x.Level))
				{
					foreach (AddProp addProp in affix.AddProps ?? new List<AddProp>())
						if (TryParseFightProp(addProp.propType, out var type))
							Add(type, (float)(addProp.value ?? 0d));
				}
			}
		}

		float maxHp = Math.Max(0f, props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_BASE_HP) *
									  Math.Max(0f, 1f + props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_HP_PERCENT)) +
									  props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_HP));
		float attack = Math.Max(0f, props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_BASE_ATTACK) *
									   Math.Max(0f, 1f + props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_ATTACK_PERCENT)) +
									   props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_ATTACK));
		float defense = Math.Max(0f, props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_BASE_DEFENSE) *
										Math.Max(0f, 1f + props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_DEFENSE_PERCENT)) +
										props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_DEFENSE));

		Set(FightPropType.FIGHT_PROP_MAX_HP, maxHp);
		Set(FightPropType.FIGHT_PROP_CUR_ATTACK, attack);
		Set(FightPropType.FIGHT_PROP_CUR_DEFENSE, defense);

		MaxHp = maxHp;
		Atk = attack;
		Def = defense;
		CritRate = props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_CRITICAL, avatarExcel.critical);
		CritDmg = props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_CRITICAL_HURT, avatarExcel.criticalHurt);
		EM = props.GetValueOrDefault((uint)FightPropType.FIGHT_PROP_ELEMENT_MASTERY);
		Hp = oldHp <= 0f ? 0f : (wasFullHp ? maxHp : Math.Min(oldHp, maxHp));
		Set(FightPropType.FIGHT_PROP_CUR_HP, Hp);

		// Only the avatar's actual element receives max/current energy props.
		if (resourceManager.AvatarSkillExcel.TryGetValue(UltSkillId, out var energySkill) &&
			TryGetEnergyProps(energySkill.costElemType, out var maxEnergyProp, out var curEnergyProp))
		{
			float maxEnergy = Math.Max(0, energySkill.costElemVal);
			Set(maxEnergyProp, maxEnergy);
			CurElemEnergy = Math.Clamp(CurElemEnergy, 0f, maxEnergy);
			Set(curEnergyProp, CurElemEnergy);
		}

		return props;
	}

	public void ReCalculateFightProps() => BuildFightPropMap();

	private static float ApplyCurve(float value, GrowCurveInfo curve)
	{
		// Weapon::assignLevelProp and Creature prop-grow code accept ADD/MULTI.
		return curve.arith switch
		{
			ArithType.ARITH_ADD => value + curve.value,
			ArithType.ARITH_MULTI => value * curve.value,
			ArithType.ARITH_SUB => value - curve.value,
			ArithType.ARITH_DIVIDE => curve.value == 0f ? value : value / curve.value,
			_ => value
		};
	}

	public float CalculateByArith(float baseValue, float growValue, ArithType arithType)
		=> ApplyCurve(baseValue, new GrowCurveInfo { value = growValue, arith = arithType });

	private Dictionary<uint, uint> GetWeaponAffixMap(PlayerWeapon weapon)
		=> new(weapon.AffixMap);

	private static bool TryParseFightProp(string? text, out FightPropType type)
	{
		type = FightPropType.FIGHT_PROP_NONE;
		return !string.IsNullOrWhiteSpace(text) && Enum.TryParse(text, out type) && type != FightPropType.FIGHT_PROP_NONE;
	}

	private static bool TryGetEnergyProps(Resource.ElementType element, out FightPropType max, out FightPropType cur)
	{
		(max, cur) = element switch
		{
			Resource.ElementType.Fire => (FightPropType.FIGHT_PROP_MAX_FIRE_ENERGY, FightPropType.FIGHT_PROP_CUR_FIRE_ENERGY),
			Resource.ElementType.Electric => (FightPropType.FIGHT_PROP_MAX_ELEC_ENERGY, FightPropType.FIGHT_PROP_CUR_ELEC_ENERGY),
			Resource.ElementType.Water => (FightPropType.FIGHT_PROP_MAX_WATER_ENERGY, FightPropType.FIGHT_PROP_CUR_WATER_ENERGY),
			Resource.ElementType.Grass => (FightPropType.FIGHT_PROP_MAX_GRASS_ENERGY, FightPropType.FIGHT_PROP_CUR_GRASS_ENERGY),
			Resource.ElementType.Wind => (FightPropType.FIGHT_PROP_MAX_WIND_ENERGY, FightPropType.FIGHT_PROP_CUR_WIND_ENERGY),
			Resource.ElementType.Ice => (FightPropType.FIGHT_PROP_MAX_ICE_ENERGY, FightPropType.FIGHT_PROP_CUR_ICE_ENERGY),
			Resource.ElementType.Rock => (FightPropType.FIGHT_PROP_MAX_ROCK_ENERGY, FightPropType.FIGHT_PROP_CUR_ROCK_ENERGY),
			_ => (FightPropType.FIGHT_PROP_NONE, FightPropType.FIGHT_PROP_NONE)
		};
		return max != FightPropType.FIGHT_PROP_NONE;
	}

	public void BroadcastPropUpdate()
	{
		var propUpdateNotify = new AvatarFightPropUpdateNotify { AvatarGuid = Guid };
		AddAllFightProps(propUpdateNotify.FightPropMaps);
		Session.SendPacket(propUpdateNotify);
	}

	public void AddAllFightProps(Dictionary<uint, float> target)
	{
		target.Clear();
		foreach (var (type, value) in BuildFightPropMap())
			target[type] = value;
	}

	private void AddPropMap(PropType propType, uint ival, Dictionary<uint, PropValue> keyValuePairs)
	{
		keyValuePairs.Add((uint)propType, new PropValue()
		{
			Type = (uint)propType,
			Ival = ival,
			Val = ival
		});
	}

	private void AddFightPropMap(FightProp propType, float val, Dictionary<uint, float> keyValuePairs)
	{
		keyValuePairs.Add((uint)propType, val);
	}
}


public enum PropType
{
	PROP_NONE = 0,
	PROP_EXP = 1001,
	PROP_BREAK_LEVEL = 1002,
	PROP_SMALL_TALENT_POINT = 1004,
	PROP_BIG_TALENT_POINT = 1005,
	PROP_GEAR_START_VAL = 2001,
	PROP_GEAR_STOP_VAL = 2002,
	PROP_LEVEL = 4001,
	PROP_LAST_CHANGE_AVATAR_TIME = 10001,
	PROP_MAX_SPRING_VOLUME = 10002,
	PROP_CUR_SPRING_VOLUME = 10003,
	PROP_IS_SPRING_AUTO_USE = 10004,
	PROP_SPRING_AUTO_USE_PERCENT = 10005,
	PROP_IS_FLYABLE = 10006,
	PROP_IS_WEATHER_LOCKED = 10007,
	PROP_IS_GAME_TIME_LOCKED = 10008,
	PROP_IS_TRANSFERABLE = 10009,
	PROP_MAX_STAMINA = 10010,
	PROP_CUR_PERSIST_STAMINA = 10011,
	PROP_CUR_TEMPORARY_STAMINA = 10012,
	PROP_PLAYER_LEVEL = 10013,
	PROP_PLAYER_EXP = 10014,
	PROP_PLAYER_HCOIN = 10015,
	PROP_PLAYER_SCOIN = 10016,
	PROP_PLAYER_MP_SETTING_TYPE = 10017,
	PROP_IS_MP_MODE_AVAILABLE = 10018,
	PROP_PLAYER_LEVEL_LOCK_ID = 10019,
	PROP_PLAYER_RESIN = 10020,
	PROP_PLAYER_WORLD_RESIN = 10021,
	PROP_PLAYER_WAIT_SUB_HCOIN = 10022,
	PROP_PLAYER_WAIT_SUB_SCOIN = 10023,
}

public enum FightProp
{
	FIGHT_PROP_NONE = 0,
	FIGHT_PROP_BASE_HP = 1,
	FIGHT_PROP_HP = 2,
	FIGHT_PROP_HP_PERCENT = 3,
	FIGHT_PROP_BASE_ATTACK = 4,
	FIGHT_PROP_ATTACK = 5,
	FIGHT_PROP_ATTACK_PERCENT = 6,
	FIGHT_PROP_BASE_DEFENSE = 7,
	FIGHT_PROP_DEFENSE = 8,
	FIGHT_PROP_DEFENSE_PERCENT = 9,
	FIGHT_PROP_BASE_SPEED = 10,
	FIGHT_PROP_SPEED_PERCENT = 11,
	FIGHT_PROP_HP_MP_PERCENT = 12,
	FIGHT_PROP_ATTACK_MP_PERCENT = 13,
	FIGHT_PROP_CRITICAL = 20,
	FIGHT_PROP_ANTI_CRITICAL = 21,
	FIGHT_PROP_CRITICAL_HURT = 22,
	FIGHT_PROP_CHARGE_EFFICIENCY = 23,
	FIGHT_PROP_ADD_HURT = 24,
	FIGHT_PROP_SUB_HURT = 25,
	FIGHT_PROP_HEAL_ADD = 26,
	FIGHT_PROP_HEALED_ADD = 27,
	FIGHT_PROP_ELEMENT_MASTERY = 28,
	FIGHT_PROP_PHYSICAL_SUB_HURT = 29,
	FIGHT_PROP_PHYSICAL_ADD_HURT = 30,
	FIGHT_PROP_DEFENCE_IGNORE_RATIO = 31,
	FIGHT_PROP_DEFENCE_IGNORE_DELTA = 32,
	FIGHT_PROP_FIRE_ADD_HURT = 40,
	FIGHT_PROP_ELEC_ADD_HURT = 41,
	FIGHT_PROP_WATER_ADD_HURT = 42,
	FIGHT_PROP_GRASS_ADD_HURT = 43,
	FIGHT_PROP_WIND_ADD_HURT = 44,
	FIGHT_PROP_ROCK_ADD_HURT = 45,
	FIGHT_PROP_ICE_ADD_HURT = 46,
	FIGHT_PROP_HIT_HEAD_ADD_HURT = 47,
	FIGHT_PROP_FIRE_SUB_HURT = 50,
	FIGHT_PROP_ELEC_SUB_HURT = 51,
	FIGHT_PROP_WATER_SUB_HURT = 52,
	FIGHT_PROP_GRASS_SUB_HURT = 53,
	FIGHT_PROP_WIND_SUB_HURT = 54,
	FIGHT_PROP_ROCK_SUB_HURT = 55,
	FIGHT_PROP_ICE_SUB_HURT = 56,
	FIGHT_PROP_EFFECT_HIT = 60,
	FIGHT_PROP_EFFECT_RESIST = 61,
	FIGHT_PROP_FREEZE_RESIST = 62,
	FIGHT_PROP_TORPOR_RESIST = 63,
	FIGHT_PROP_DIZZY_RESIST = 64,
	FIGHT_PROP_FREEZE_SHORTEN = 65,
	FIGHT_PROP_TORPOR_SHORTEN = 66,
	FIGHT_PROP_DIZZY_SHORTEN = 67,
	FIGHT_PROP_MAX_FIRE_ENERGY = 70,
	FIGHT_PROP_MAX_ELEC_ENERGY = 71,
	FIGHT_PROP_MAX_WATER_ENERGY = 72,
	FIGHT_PROP_MAX_GRASS_ENERGY = 73,
	FIGHT_PROP_MAX_WIND_ENERGY = 74,
	FIGHT_PROP_MAX_ICE_ENERGY = 75,
	FIGHT_PROP_MAX_ROCK_ENERGY = 76,
	FIGHT_PROP_SKILL_CD_MINUS_RATIO = 80,
	FIGHT_PROP_SHIELD_COST_MINUS_RATIO = 81,
	FIGHT_PROP_CUR_FIRE_ENERGY = 1000,
	FIGHT_PROP_CUR_ELEC_ENERGY = 1001,
	FIGHT_PROP_CUR_WATER_ENERGY = 1002,
	FIGHT_PROP_CUR_GRASS_ENERGY = 1003,
	FIGHT_PROP_CUR_WIND_ENERGY = 1004,
	FIGHT_PROP_CUR_ICE_ENERGY = 1005,
	FIGHT_PROP_CUR_ROCK_ENERGY = 1006,
	FIGHT_PROP_CUR_HP = 1010,
	FIGHT_PROP_MAX_HP = 2000,
	FIGHT_PROP_CUR_ATTACK = 2001,
	FIGHT_PROP_CUR_DEFENSE = 2002,
	FIGHT_PROP_CUR_SPEED = 2003,
}

public sealed class AvatarSkillRuntimeState
{
	public uint PassCdTime { get; set; }
	public List<uint> FullCdTimes { get; } = new();
	public uint? MaxChargeCount { get; set; }
}
