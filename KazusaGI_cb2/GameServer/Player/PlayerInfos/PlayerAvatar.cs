using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Avatar;
using KazusaGI_cb2.Resource.Json.Talent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

public class PlayerAvatar
{
    private static ResourceManager resourceManager = MainApp.resourceManager;
    private Session Session { get; set; }
    public AvatarExcelConfig avatarExcel { get; set; }
	public AvatarSkillDepotExcelConfig avatarSkillDepotExcel { get; set; }
    public ulong Guid { get; set; } // critical
    public uint AvatarId { get; set; } // critical
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

	public PlayerAvatar(Session session, uint AvatarId)
    {
        this.Session = session;
        this.avatarExcel = resourceManager.AvatarExcel[AvatarId];
        this.SkillDepotId = this.isTraveler() ? avatarExcel.candSkillDepotIds[3] : avatarExcel.skillDepotId;
        this.avatarSkillDepotExcel = resourceManager.AvatarSkillDepotExcel[this.SkillDepotId];
        this.UltSkillId = avatarSkillDepotExcel.energySkill;
        this.SkillLevels = new Dictionary<uint, uint>();
        this.UnlockedTalents = new HashSet<uint>();
        this.ProudSkills = new HashSet<uint>();
        this.Guid = session.GetGuid();
        this.AvatarId = AvatarId;
        this.Level = 90;
        this.Exp = 0;
        this.BreakLevel = 4;
        this.Hp = avatarExcel.hpBase;
        this.MaxHp = avatarExcel.hpBase;
        this.Def = avatarExcel.defenseBase;
        this.Atk = avatarExcel.attackBase;
        this.CritRate = avatarExcel.critical;
        this.CritDmg = avatarExcel.criticalHurt;
        this.EM = 100;
        this.PromoteLevel = 6;
        this.CurElemEnergy = 0;
        uint initialWeapon = avatarExcel.initialWeapon;
        if (initialWeapon != 0)
        {
            PlayerWeapon weapon = new(session, initialWeapon);
			weapon.EquipOnAvatar(this, false);
        }
        this.SkillLevels.Add(this.UltSkillId, 1); // todo: get from resources
        foreach (uint skillId in avatarSkillDepotExcel.skills)
        {
            if (skillId == 0) continue;
            this.SkillLevels.Add(skillId, 1); // todo: get from resources
        }
        foreach (uint talentId in avatarSkillDepotExcel.talents)
        {
            if (talentId == 0) continue;
            this.UnlockedTalents.Add(talentId);
        }
        foreach(ProudSkillOpenConfig proudSkillOpenConfig in this.avatarSkillDepotExcel.inherentProudSkillOpens)
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
			string name = $"Avatar_{AvatarName}";
			if (!resourceManager.ConfigAvatarMap.TryGetValue($"ConfigAvatar_{AvatarName}", out ConfigAvatar? configAvatar))
			{
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
			if (resourceManager.AvatarTalentConfigDataMap.TryGetValue($"ConfigTalent_{Regex.Replace(name, "Avatar_", "")}", out Dictionary<string, BaseConfigTalent[]>? configTalents))
				ConfigTalentMap[(int)depot.id] = configTalents;
			Dictionary<uint, ConfigAbility> abilityHashMap = new();
			foreach (TargetAbility ability in resourceManager.ConfigAvatarMap[$"ConfigAvatar_{AvatarName}"].abilities)
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

    public SceneAvatarInfo ToSceneAvatarInfo()
    {
        SceneAvatarInfo sceneAvatarInfo = new SceneAvatarInfo()
        {
            PeerId = 1,
            Guid = this.Guid,
            AvatarId = this.AvatarId,
            Uid = Session.player!.Uid,
            SkillDepotId = this.SkillDepotId,
            CoreProudSkillLevel = 1,
            Weapon = Session.player!.weaponDict[this.EquipGuid].ToSceneWeaponInfo(Session),
        };
        foreach (uint unlockedTalentId in this.UnlockedTalents)
        {
            sceneAvatarInfo.TalentIdLists.Add(unlockedTalentId);
        }
        foreach (uint proudSkillId in this.ProudSkills)
        {
            sceneAvatarInfo.InherentProudSkillLists.Add(proudSkillId);
        }
        foreach (KeyValuePair<uint, uint> skillLevelMapKvp in this.SkillLevels)
        {
            uint skillId = skillLevelMapKvp.Key;
            uint level = skillLevelMapKvp.Value;

            sceneAvatarInfo.SkillLevelMaps.Add(skillId, level);
        }
        sceneAvatarInfo.EquipIdLists.Add(Session.player!.weaponDict[this.EquipGuid].WeaponId);
        return sceneAvatarInfo;
    }

    public bool isTraveler()
    {
        return this.avatarExcel.candSkillDepotIds.Count > 0;
    }

    public AvatarInfo ToAvatarInfo()
    {
        AvatarInfo avatarInfo = new AvatarInfo()
        {
            Guid = this.Guid,
            AvatarId = this.AvatarId,
            LifeState = this.Hp > 0 ? (uint)1 : 0,
            SkillDepotId = this.SkillDepotId,
            AvatarType = (uint)AvatarType.AvatarTypeFormal,
            CoreProudSkillLevel = 1,
            FetterInfo = new AvatarFetterInfo()
            {
                ExpLevel = 10,
                ExpNumber = 10
            }
        };
        foreach (uint unlockedTalentId in this.UnlockedTalents)
        {
            avatarInfo.TalentIdLists.Add(unlockedTalentId);
        }
        foreach (uint proudSkillId in this.ProudSkills)
        {
            avatarInfo.InherentProudSkillLists.Add(proudSkillId);
        }
        foreach (KeyValuePair<uint, uint> skillLevelMapKvp in this.SkillLevels)
        {
            uint skillId = skillLevelMapKvp.Key;
            uint level = skillLevelMapKvp.Value;

            avatarInfo.SkillLevelMaps.Add(skillId, level);
        }
        AddPropMap(PropType.PROP_LEVEL, this.Level, avatarInfo.PropMaps);
        AddPropMap(PropType.PROP_EXP, this.Exp, avatarInfo.PropMaps);
        AddPropMap(PropType.PROP_BREAK_LEVEL, this.BreakLevel, avatarInfo.PropMaps);

        ReCalculateFightProps();

        AddAllFightProps(avatarInfo.FightPropMaps);

        avatarInfo.EquipGuidLists.Add(this.EquipGuid);
        return avatarInfo;
    }

    // todo: reliquary
    public void ReCalculateFightProps()
    {
        // needed for the avatar curve calculations
        PlayerWeapon weapon = this.Session.player!.weaponDict[this.EquipGuid];
        WeaponExcelConfig weaponExcel = MainApp.resourceManager.WeaponExcel[weapon.WeaponId];
        AvatarCurveExcelConfig curveConfig = resourceManager.AvatarCurveExcel[this.Level];
        WeaponCurveExcelConfig weaponCurveConfig = resourceManager.WeaponCurveExcel[weapon.Level];

        // init
        float baseCritRate = this.avatarExcel.critical;
        float baseCritDmg = this.avatarExcel.criticalHurt;
        float baseHp = this.avatarExcel.hpBase;
        float baseAtk = this.avatarExcel.attackBase;
        float baseDef = this.avatarExcel.defenseBase;
        float baseElemMastery = this.EM;

        // start with HP
        // first we do character curves
        GrowCurveType growCurveType = this.avatarExcel.propGrowCurves.Find(c => c.type == FightPropType.FIGHT_PROP_BASE_HP)!.growCurve;
        GrowCurveInfo growCurve_Avatar_hp = curveConfig.curveInfos.Find(c => c.type == growCurveType)!;
        baseHp = CalculateByArith(baseHp, growCurve_Avatar_hp.value, growCurve_Avatar_hp.arith);

        // then we do weapon curves
        WeaponProperty? weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_HP_PERCENT);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_hp = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_hp.value != 0)
            {
                // since this is a percentage, we need to multiply it by 100 to get the actual value (delta)
                baseHp = CalculateByArith(baseHp, growCurve_Weapon_hp.value * 100, growCurve_Weapon_hp.arith);
            }
        }

        // idk if its even nessessary to reset the weaponProperty, but just in case
        weaponProperty = null; // reset

        // then we do Atk
        growCurveType = this.avatarExcel.propGrowCurves.Find(c => c.type == FightPropType.FIGHT_PROP_BASE_ATTACK)!.growCurve;
        GrowCurveInfo growCurve_Avatar_atk = curveConfig.curveInfos.Find(c => c.type == growCurveType)!;
        baseAtk = CalculateByArith(baseAtk, growCurve_Avatar_atk.value, growCurve_Avatar_atk.arith);

        // then we do weapon curves
        // start with FIGHT_PROP_BASE_ATTACK
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_BASE_ATTACK);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_atk = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_atk.value != 0f)
            {
                baseAtk += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_atk.value, growCurve_Weapon_atk.arith);
            }
        }

        weaponProperty = null; // reset

        // and then FIGHT_PROP_ATTACK_PERCENT
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_ATTACK_PERCENT);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_atk = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_atk.value != 0f)
            {
                // since this is a percentage, we need to multiply it by 100 to get the actual value (delta)
                baseAtk += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_atk.value * 100, growCurve_Weapon_atk.arith);
            }
        }

        weaponProperty = null; // reset

        // then we do Def
        growCurveType = this.avatarExcel.propGrowCurves.Find(c => c.type == FightPropType.FIGHT_PROP_BASE_DEFENSE)!.growCurve;
        GrowCurveInfo growCurve_Avatar_def = curveConfig.curveInfos.Find(c => c.type == growCurveType)!;
        baseDef = CalculateByArith(baseDef, growCurve_Avatar_def.value, growCurve_Avatar_def.arith);

        // then we do weapon curves
        // start with FIGHT_PROP_BASE_DEFENSE
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_BASE_DEFENSE);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_def = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_def.value != 0f)
            {
                baseDef += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_def.value, growCurve_Weapon_def.arith);
            }
        }

        // and then FIGHT_PROP_DEFENSE_PERCENT
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_DEFENSE_PERCENT);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_def = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_def.value != 0f)
            {
                // since this is a percentage, we need to multiply it by 100 to get the actual value (delta)
                baseDef += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_def.value * 100, growCurve_Weapon_def.arith);
            }
        }

        // then we do CritRate
        // weapon
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_CRITICAL);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_crit = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_crit.value != 0f)
            {
                baseCritRate += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_crit.value, growCurve_Weapon_crit.arith);
            }
        }

        // then we do CritDmg
        // weapon
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_CRITICAL_HURT);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_critDmg = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_critDmg.value != 0f)
            {
                baseCritDmg += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_critDmg.value, growCurve_Weapon_critDmg.arith);
            }
        }

        // then we do EM
        // weapon
        weaponProperty = weaponExcel.weaponProp.FirstOrDefault(c => c.propType == FightPropType.FIGHT_PROP_ELEMENT_MASTERY);
        if (weaponProperty != null)
        {
            growCurveType = weaponProperty.type;
            GrowCurveInfo growCurve_Weapon_EM = weaponCurveConfig.curveInfos.Find(c => c.type == growCurveType)!;
            if (growCurve_Weapon_EM.value != 0f)
            {
                baseElemMastery += CalculateByArith(weaponProperty.initValue, growCurve_Weapon_EM.value, growCurve_Weapon_EM.arith);
            }
        }

        // set the values
        this.MaxHp = baseHp;
        this.Atk = baseAtk;
        this.Def = baseDef;
        this.CritRate = baseCritRate;
        this.CritDmg = baseCritDmg;
        this.EM = baseElemMastery;
        this.Hp = baseHp;
    }

    public float CalculateByArith(float baseValue, float growValue, ArithType arithType)
    {
        switch (arithType)
        {
            case ArithType.ARITH_MULTI:
                return baseValue + baseValue * growValue;
            case ArithType.ARITH_ADD:
                return baseValue + growValue;
            case ArithType.ARITH_SUB:
                return baseValue - growValue;
            case ArithType.ARITH_DIVIDE:
                return baseValue - baseValue / growValue;
            default:
                return baseValue;
        }
    }

    public void BroadcastPropUpdate()
    {
        var propUpdateNotify = new AvatarFightPropUpdateNotify()
        {
            AvatarGuid = this.Guid
        };
        AddAllFightProps(propUpdateNotify.FightPropMaps);
        Session.SendPacket(propUpdateNotify);
	}

	public void AddAllFightProps(Dictionary<uint, float> keyValuePairs)
    {
        AvatarExcelConfig avatarExcel = MainApp.resourceManager.AvatarExcel[this.AvatarId];
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_HP, this.MaxHp, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_HP_PERCENT, 0.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_SPEED, 0.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_BASE_DEFENSE, avatarExcel.defenseBase, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_BASE_ATTACK, avatarExcel.attackBase, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CRITICAL, this.CritRate, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CRITICAL_HURT, this.CritDmg, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CHARGE_EFFICIENCY, 2.0f, keyValuePairs); // its a ps, let people have fun
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_ROCK_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_ICE_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_WATER_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_FIRE_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_ELEC_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_GRASS_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_MAX_WIND_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_ROCK_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_ICE_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_WATER_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_ELEC_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_FIRE_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_WIND_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_GRASS_ENERGY, 100.0f, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_HP, this.Hp, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_BASE_HP, avatarExcel.hpBase, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_DEFENSE, this.Def, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_CUR_ATTACK, this.Atk, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_ELEMENT_MASTERY, this.EM, keyValuePairs);
        AddFightPropMap(FightProp.FIGHT_PROP_SKILL_CD_MINUS_RATIO, 2.0f, keyValuePairs); // its a ps, let people have fun
    }

    private void AddPropMap(PropType propType, uint ival, Dictionary<uint,PropValue> keyValuePairs)
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