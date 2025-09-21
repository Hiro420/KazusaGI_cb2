using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource;

[JsonConverter(typeof(StringEnumConverter))]
public enum AvatarUseType
{
    AVATAR_TEST = 0,
    AVATAR_SYNC_TEST = 1,
    AVATAR_FORMAL = 2,
    AVATAR_ABANDON = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum BodyType
{
	BODY_NONE = 0,
	BODY_BOY = 1,
	BODY_GIRL = 2,
	BODY_LADY = 3,
	BODY_MALE = 4,
	BODY_LOLI = 5
}

[JsonConverter(typeof(StringEnumConverter))]
public enum QualityType
{
	QUALITY_NONE = 0,
	QUALITY_WHITE = 1,
	QUALITY_GREEN = 2,
	QUALITY_BLUE = 3,
	QUALITY_PURPLE = 4,
	QUALITY_ORANGE = 5
}

[JsonConverter(typeof(StringEnumConverter))]
public enum WeaponType
{
	WEAPON_SWORD_ONE_HAND = 1,
	WEAPON_CROSSBOW = 2,
	WEAPON_STAFF = 3,
	WEAPON_DOUBLE_DAGGER = 4,
	WEAPON_KATANA = 5,
	WEAPON_SHURIKEN = 6,
	WEAPON_STICK = 7,
	WEAPON_SPEAR = 8,
	WEAPON_SHIELD_SMALL = 9,
	WEAPON_CATALYST = 10,
	WEAPON_CLAYMORE = 11,
	WEAPON_BOW = 12,
	WEAPON_POLE = 13
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AvatarIdentityType
{
    AVATAR_IDENTITY_MASTER = 0,
    AVATAR_IDENTITY_NORMAL = 1
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ScenePointType
{
    NORMAL = 0,
    TOWER = 1,
    PORTAL = 2,
    Other = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum MonsterType
{
    MONSTER_NONE = 0,
    MONSTER_ORDINARY = 1,
    MONSTER_BOSS = 2,
    MONSTER_ENV_ANIMAL = 3,
    MONSTER_LITTLE_MONSTER = 4,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum FightPropType
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
	FIGHT_PROP_CUR_SPEED = 2003
}

[JsonConverter(typeof(StringEnumConverter))]
public enum GrowCurveType
{
	GROW_CURVE_NONE = 0,
	GROW_CURVE_HP = 1,
	GROW_CURVE_ATTACK = 2,
	GROW_CURVE_STAMINA = 3,
	GROW_CURVE_STRIKE = 4,
	GROW_CURVE_ANTI_STRIKE = 5,
	GROW_CURVE_ANTI_STRIKE1 = 6,
	GROW_CURVE_ANTI_STRIKE2 = 7,
	GROW_CURVE_ANTI_STRIKE3 = 8,
	GROW_CURVE_STRIKE_HURT = 9,
	GROW_CURVE_ELEMENT = 10,
	GROW_CURVE_KILL_EXP = 11,
	GROW_CURVE_DEFENSE = 12,
	GROW_CURVE_ATTACK_BOMB = 13,
	GROW_CURVE_HP_LITTLEMONSTER = 14,
	GROW_CURVE_ELEMENT_MASTERY = 15,
	GROW_CURVE_PROGRESSION = 16,
	GROW_CURVE_DEFENDING = 17,
	GROW_CURVE_HP_S5 = 21,
	GROW_CURVE_HP_S4 = 22,
	GROW_CURVE_ATTACK_S5 = 31,
	GROW_CURVE_ATTACK_S4 = 32,
	GROW_CURVE_ATTACK_S3 = 33,
	GROW_CURVE_STRIKE_S5 = 34,
	GROW_CURVE_DEFENSE_S5 = 41,
	GROW_CURVE_DEFENSE_S4 = 42,
	GROW_CURVE_ATTACK_101 = 1101,
	GROW_CURVE_ATTACK_102 = 1102,
	GROW_CURVE_ATTACK_103 = 1103,
	GROW_CURVE_ATTACK_104 = 1104,
	GROW_CURVE_ATTACK_105 = 1105,
	GROW_CURVE_ATTACK_201 = 1201,
	GROW_CURVE_ATTACK_202 = 1202,
	GROW_CURVE_ATTACK_203 = 1203,
	GROW_CURVE_ATTACK_204 = 1204,
	GROW_CURVE_ATTACK_205 = 1205,
	GROW_CURVE_ATTACK_301 = 1301,
	GROW_CURVE_ATTACK_302 = 1302,
	GROW_CURVE_ATTACK_303 = 1303,
	GROW_CURVE_ATTACK_304 = 1304,
	GROW_CURVE_ATTACK_305 = 1305,
	GROW_CURVE_CRITICAL_101 = 2101,
	GROW_CURVE_CRITICAL_102 = 2102,
	GROW_CURVE_CRITICAL_103 = 2103,
	GROW_CURVE_CRITICAL_104 = 2104,
	GROW_CURVE_CRITICAL_105 = 2105,
	GROW_CURVE_CRITICAL_201 = 2201,
	GROW_CURVE_CRITICAL_202 = 2202,
	GROW_CURVE_CRITICAL_203 = 2203,
	GROW_CURVE_CRITICAL_204 = 2204,
	GROW_CURVE_CRITICAL_205 = 2205,
	GROW_CURVE_CRITICAL_301 = 2301,
	GROW_CURVE_CRITICAL_302 = 2302,
	GROW_CURVE_CRITICAL_303 = 2303,
	GROW_CURVE_CRITICAL_304 = 2304,
	GROW_CURVE_CRITICAL_305 = 2305
}

[JsonConverter(typeof(StringEnumConverter))]
public enum GachaItemType
{
    GACHA_ITEM_INVALID = 0,
    GACHA_ITEM_AVATAR_S5 = 11,
    GACHA_ITEM_AVATAR_S4 = 12,
    GACHA_ITEM_AVATAR_S3 = 13,
    GACHA_ITEM_WEAPON_S5 = 21,
    GACHA_ITEM_WEAPON_S4 = 22,
    GACHA_ITEM_WEAPON_S3 = 23,
    GACHA_ITEM_COMMON_MATERIAL = 31
}

[JsonConverter(typeof(StringEnumConverter))]
public enum MaterialType
{
    MATERIAL_NONE = 0,
    MATERIAL_FOOD = 1,
    MATERIAL_QUEST = 2,
    MATERIAL_EXCHANGE = 4,
    MATERIAL_CONSUME = 5,
    MATERIAL_EXP_FRUIT = 6,
    MATERIAL_AVATAR = 7,
    MATERIAL_ADSORBATE = 8,
    MATERIAL_CRICKET = 9,
    MATERIAL_ELEM_CRYSTAL = 10,
    MATERIAL_WEAPON_EXP_STONE = 11,
    MATERIAL_CHEST = 12,
    MATERIAL_RELIQUARY_MATERIAL = 13,
    MATERIAL_AVATAR_MATERIAL = 14,
    MATERIAL_NOTICE_ADD_HP = 15,
    MATERIAL_SEA_LAMP = 16
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ItemType
{
    ITEM_NONE = 0,
    ITEM_VIRTUAL = 1,
    ITEM_MATERIAL = 2,
    ITEM_RELIQUARY = 3,
    ITEM_WEAPON = 4,
    ITEM_DISPLAY = 5
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ArithType
{
    ARITH_NONE = 0,
    ARITH_ADD = 1,
    ARITH_MULTI = 2,
    ARITH_SUB = 3,
    ARITH_DIVIDE = 4
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ShopType
{
    SHOP_TYPE_NONE = 0,
    SHOP_TYPE_PAIMON = 1001,
    SHOP_TYPE_CITY = 1002,
    SHOP_TYPE_BLACKSMITH = 1003,
    SHOP_TYPE_GROCERY = 1004,
    SHOP_TYPE_FOOD = 1005,
    SHOP_TYPE_SEA_LAMP = 1006,
    SHOP_TYPE_VIRTUAL_SHOP = 1007,
    SHOP_TYPE_LIYUE_GROCERY = 1008,
    SHOP_TYPE_LIYUE_SOUVENIR = 1009,
    SHOP_TYPE_LIYUE_RESTAURANT = 1010,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum OpenStateType
{
    OPEN_STATE_NONE = 0,
    OPEN_STATE_PAIMON = 1,
    OPEN_STATE_PAIMON_NAVIGATION = 2,
    OPEN_STATE_AVATAR_PROMOTE = 3,
    OPEN_STATE_AVATAR_TALENT = 4,
    OPEN_STATE_WEAPON_PROMOTE = 5,
    OPEN_STATE_WEAPON_AWAKEN = 6,
    OPEN_STATE_QUEST_REMIND = 7,
    OPEN_STATE_GAME_GUIDE = 8,
    OPEN_STATE_COOK = 9,
    OPEN_STATE_WEAPON_UPGRADE = 10,
    OPEN_STATE_RELIQUARY_UPGRADE = 11,
    OPEN_STATE_RELIQUARY_PROMOTE = 12,
    OPEN_STATE_WEAPON_PROMOTE_GUIDE = 13,
    OPEN_STATE_WEAPON_CHANGE_GUIDE = 14,
    OPEN_STATE_PLAYER_LVUP_GUIDE = 15,
    OPEN_STATE_FRESHMAN_GUIDE = 16,
    OPEN_STATE_SKIP_FRESHMAN_GUIDE = 17,
    OPEN_STATE_GUIDE_MOVE_CAMERA = 18,
    OPEN_STATE_GUIDE_SCALE_CAMERA = 19,
    OPEN_STATE_GUIDE_KEYBOARD = 20,
    OPEN_STATE_GUIDE_MOVE = 21,
    OPEN_STATE_GUIDE_JUMP = 22,
    OPEN_STATE_GUIDE_SPRINT = 23,
    OPEN_STATE_GUIDE_MAP = 24,
    OPEN_STATE_GUIDE_ATTACK = 25,
    OPEN_STATE_GUIDE_FLY = 26,
    OPEN_STATE_GUIDE_TALENT = 27,
    OPEN_STATE_GUIDE_RELIC = 28,
    OPEN_STATE_GUIDE_RELIC_PROM = 29,
    OPEN_STATE_COMBINE = 30,
    OPEN_STATE_GACHA = 31,
    OPEN_STATE_GUIDE_GACHA = 32,
    OPEN_STATE_GUIDE_TEAM = 33,
    OPEN_STATE_GUIDE_PROUD = 34,
    OPEN_STATE_GUIDE_AVATAR_PROMOTE = 35,
    OPEN_STATE_GUIDE_ADVENTURE_CARD = 36,
    OPEN_STATE_FORGE = 37,
    OPEN_STATE_GUIDE_BAG = 38,
    OPEN_STATE_EXPEDITION = 39,
    OPEN_STATE_GUIDE_ADVENTURE_DAILYTASK = 40,
    OPEN_STATE_GUIDE_ADVENTURE_DUNGEON = 41,
    OPEN_STATE_TOWER = 42,
    OPEN_STATE_WORLD_STAMINA = 43,
    OPEN_STATE_TOWER_FIRST_ENTER = 44,
    OPEN_STATE_RESIN = 45,
    OPEN_STATE_WORLD_RESIN = 46,
    OPEN_STATE_LIMIT_REGION_FRESHMEAT = 47,
    OPEN_STATE_LIMIT_REGION_GLOBAL = 48,
    OPEN_STATE_MULTIPLAYER = 49,
    OPEN_STATE_GUIDE_MOUSEPC = 50,
    OPEN_STATE_GUIDE_MULTIPLAYER = 51,
    OPEN_STATE_GUIDE_DUNGEONREWARD = 52,
    OPEN_STATE_SHOP_TYPE_PAIMON = 1001,
    OPEN_STATE_SHOP_TYPE_CITY = 1002,
    OPEN_STATE_SHOP_TYPE_BLACKSMITH = 1003,
    OPEN_STATE_SHOP_TYPE_GROCERY = 1004,
    OPEN_STATE_SHOP_TYPE_FOOD = 1005,
    OPEN_STATE_SHOP_TYPE_SEA_LAMP = 1006,
    OPEN_STATE_SHOP_TYPE_VIRTUAL_SHOP = 1007,
    OPEN_STATE_SHOP_TYPE_LIYUE_GROCERY = 1008,
    OPEN_STATE_SHOP_TYPE_LIYUE_SOUVENIR = 1009,
    OPEN_STATE_SHOP_TYPE_LIYUE_RESTAURANT = 1010,
    OPEN_ADVENTURE_MANUAL = 1100,
    OPEN_ADVENTURE_MANUAL_CITY_MENGDE = 1101,
    OPEN_ADVENTURE_MANUAL_CITY_LIYUE = 1102,
    OPEN_ADVENTURE_MANUAL_MONSTER = 1103,
    OPEN_STATE_ACTIVITY_SEALAMP = 1200,
    OPEN_STATE_ACTIVITY_SEALAMP_TAB2 = 1201,
    OPEN_STATE_ACTIVITY_SEALAMP_TAB3 = 1202,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum DungeonType
{
    DUNGEON_NONE = 0,
    DUNGEON_PLOT = 1,
    DUNGEON_FIGHT = 2,
    DUNGEON_DAILY_FIGHT = 3,
    DUNGEON_WEEKLY_FIGHT = 4,
    DUNGEON_DISCARDED = 5,
    DUNGEON_TOWER = 6,
    DUNGEON_BOSS = 7,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum GadgetState
{
    Default = 0,
    GatherDrop = 1,
    ChestLocked = 101,
    ChestOpened = 102,
    ChestTrap = 103,
    ChestBramble = 104,
    ChestFrozen = 105,
    ChestRock = 106,
    GearStart = 201,
    GearStop = 202,
    GearAction1 = 203,
    GearAction2 = 204,
    CrystalResonate1 = 301,
    CrystalResonate2 = 302,
    CrystalExplode = 303,
    CrystalDrain = 304,
    StatueActive = 401,
    Action01 = 901,
    Action02 = 902,
    Action03 = 903
}

[JsonConverter(typeof(StringEnumConverter))]
public enum GadgetType_Lua
{
    GADGET_NONE = 0,
    GADGET_WORLD_CHECT = 1,
    GADGET_DUNGEON_SECRET_CHEST = 2,
    GADGET_DUNGEON_PASS_CHEST = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TowerCondType
{
    TOWER_COND_NONE = 0,
    TOWER_COND_FINISH_TIME_LESS_THAN = 1,
    TOWER_COND_LEFT_HP_GREATER_THAN = 2,
    TOWER_COND_CHALLENGE_LEFT_TIME_MORE_THAN = 3,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum GroupKillPolicy
{
    GROUP_KILL_NONE = 0,
    GROUP_KILL_ALL = 1,
    GROUP_KILL_MONSTER = 2,
    GROUP_KILL_GADGET = 3,
    GROUP_KILL_NPC = 4
}

[JsonConverter(typeof(StringEnumConverter))]
public enum RegionShape
{
    REGION_NONE = 0,
    REGION_SPHERE = 1,
    REGION_CUBIC = 2
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EntityType
{
    None = 0,
    Avatar = 1,
    Monster = 2,
    Bullet = 3,
    AttackPhyisicalUnit = 4,
    AOE = 5,
    Camera = 6,
    EnviroArea = 7,
    Equip = 8,
    MonsterEquip = 9,
    Grass = 10,
    Level = 11,
    NPC = 12,
    TransPointFirst = 13,
    TransPointFirstGadget = 14,
    TransPointSecond = 15,
    TransPointSecondGadget = 16,
    DropItem = 17,
    Field = 18,
    Gadget = 19,
    Water = 20,
    GatherPoint = 21,
    GatherObject = 22,
    AirflowField = 23,
    SpeedupField = 24,
    Gear = 25,
    Chest = 26,
    EnergyBall = 27,
    ElemCrystal = 28,
    Timeline = 29,
    Worktop = 30,
    Team = 31,
    Platform = 32,
    AmberWind = 33,
    EnvAnimal = 34,
    SealGadget = 35,
    Tree = 36,
    Bush = 37,
    QuestGadget = 38,
    Lightning = 39,
    RewardPoint = 40,
    RewardStatue = 41,
    MPLevel = 42,
    WindSeed = 43,
    PlaceHolder = 99
}

[JsonConverter(typeof(StringEnumConverter))]
public enum GearType
{
    None = 0,
    Ray = 1,
    Spray = 2,
    Wall = 3
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EquipType
{
	EQUIP_NONE = 0,
	EQUIP_BRACER = 1,
	EQUIP_NECKLACE = 2,
	EQUIP_SHOES = 3,
	EQUIP_RING = 4,
	EQUIP_DRESS = 5,
	EQUIP_WEAPON = 6
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ElementType
{
	None = 0,
	Fire = 1,
	Water = 2,
	Grass = 3,
	Electric = 4,
	Ice = 5,
	Frozen = 6,
	Wind = 7,
	Rock = 8,
	AntiFire = 9
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TriggerType
{
	TriggerOnce = 0,
	TriggerNoRepeat = 1,
	TriggerAlways = 2
}

public enum ConfigAbilitySubContainerType : uint
{
	NONE,
	ACTION,
	MIXIN,
	MODIFIER_ACTION,
	MODIFIER_MIXIN
}

[JsonConverter(typeof(StringEnumConverter))]
public enum StackingType
{
	None,
	Unique,
	Multiple,
	MultipleAllRefresh,
	MultipleRefresh,
	MultipleRefreshNoRemove,
	Overlap,
	Refresh,
	RefreshUniqueDurability,
	RefreshAndAddDurability,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum PropertyModifierType
{
	None,
	Actor_PhysicalMuteHurtDelta,
	Actor_RockMuteHurtDelta,
	Actor_IceMuteHurtDelta,
	Actor_WindMuteHurtDelta,
	Actor_GrassMuteHurtDelta,
	Actor_CriticalHurtDelta,
	Actor_CostStaminaRatio,
	Animator_MoveSpeedRatio,
	Actor_SkillCDMinusRatio,
	Actor_AttackRatio,
	Actor_HealedAddDelta,
	Actor_ShieldCostMinusRatio,
	Actor_FireSubHurtDelta,
	Actor_ElecSubHurtDelta,
	Actor_WaterSubHurtDelta,
	Actor_GrassSubHurtDelta,
	Actor_WindSubHurtDelta,
	Actor_IceSubHurtDelta,
	Actor_RockSubHurtDelta,
	Actor_PhysicalSubHurtDelta,
	Actor_EndureDelta,
	Actor_MaxHPRatio,
	Actor_RecoverStaminaRatio,
	Actor_AddGravityScale,
	Animator_OverallSpeedRatioMultiplied,
	Actor_BulletMoveAngularVelocityRatio,
	Animator_AttackSpeedRatio,
	Entity_WeightRatio,
	Actor_DefenceRatio,
	Animator_OverallSpeedRatio,
	Actor_MaxHPDelta,
	Actor_DefenceDelta,
	Actor_AdsorbatePickRadiusDelta,
	Actor_PhysicalAddHurtDelta,
	Actor_FireAddHurtDelta,
	Actor_AttackExtraDelta,
	Actor_AttackDelta,
	Actor_FallingDamageRatio,
	Actor_CriticalDelta,
	Actor_WaterAddHurtDelta,
	Actor_GrassAddHurtDelta,
	Actor_CriticalExtraDelta,
	Actor_BulletMoveSurroundRadiusRatio,
	Actor_ElecAddHurtDelta,
	Actor_RockAddHurtDelta,
	Actor_SubHurtDelta,
	Actor_ChargeEfficiencyDelta,
	Actor_IceAddHurtDelta,
	Actor_AddHurtDelta,
	Actor_HealAddDelta,
	Actor_EnergyCostDelta,
	Actor_ElemMasteryDelta,
	Actor_AntiCriticalDelta,
	Actor_FireAddHurtExtraDelta,
	Actor_ElemMasteryExtraDelta,
	Actor_WaterAddHurtExtraDelta,
	Actor_ElemReactElectricDelta,
	Actor_WindAddHurtDelta,
	Actor_ElemReactSteamDelta,
	Actor_ChargeEfficiencyExtraDelta,
	Actor_ElemReactSwirlWaterDelta,
	Actor_ElemReactFreezeDelta,
	Actor_ElecAddHurtExtraDelta,
	Actor_DefenseIgnoreRatio,
	Actor_IceAddHurtExtraDelta,
	Entity_LifeTimeDelta,
	Actor_FlyBackSpeedMaxRatio,
	Actor_FlyHorizontalSpeedMaxRatio,
	Actor_FlyRotationAngularVelocityRatio,
	Actor_ElemAttackByRockRatio,
	Actor_FlyDownSpeedRatio,
	Actor_FlyForwardSpeedMaxRatio,
	Actor_MaxStaminaDelta,
	Entity_MassRatio,
	Actor_HpThresholdRatio,
	Actor_ElemReactSwirlFireDelta,
	Actor_ElemReactSwirlIceDelta,
	Actor_ElemReactSwirlElectricDelta,
	Actor_ElemReactSConductDelta,
	Actor_ElemReactMeltDelta,
	Actor_ElemReactExplodeDelta,
	Actor_ElemReactBurnDelta,
	Actor_FireMuteHurtDelta,
	Actor_ElecMuteHurtDelta,
	Actor_WaterMuteHurtDelta,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TargetType
{
	None,
	Owner,
	All,
	AllExceptSelf,
	Applier,
	Alliance,
	CurTeamAvatars,
	AllPlayerAvatars,
	Self,
	SelfAttackTarget,
	SelfCamp,
	OriginOwner,
	Caster,
	Other,
	Team,
	Enemy,
	Target,
	CurLocalAvatar,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum LogicType
{
	None,
	Equal,
	Greater,
	GreaterOrEqual,
	Lesser,
	LesserOrEqual,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum CompareType
{
	None,
	XYZ,
	XZ,
	Y,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VisionType
{
	// Copied from protos
	None = 0,
	Meet = 1,
	Reborn = 2,
	Replace = 3,
	WaypointReborn = 4,
	Miss = 5,
	Die = 6,
	GatherEscape = 7,
	Refresh = 8,
	Transport = 9,
	ReplaceDie = 10,
	ReplaceNoNotify = 11,
	Born = 12,
	Pickup = 13,
	Remove = 14,
	ChangeCostume = 15,
	FishRefresh = 16,
	FishBigShock = 17,
	FishQteSucc = 18,
	Unk2700Epfkmoipadb = 19,

	// Used in binout
	VisionReborn = 2,
	VisionReplaceDie = 10,
}


[JsonConverter(typeof(StringEnumConverter))]
public enum AbilityState
{
	ElementFreeze,
	ElementPetrifaction,
	ElementWet,
	Invincible,
	Limbo,
	LockHP,
	MuteTaunt,
	ElementBurning,
	Struggle,
	ElementFrozen,
}

public enum LuaCallType
{
	CurChallengeGroup,
	CurGalleryControlGroup,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum VelocityForceType
{
	RushMoveType,
	InertiaType,
	RetreatAirType,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum AvatarEventType
{
	None = 0,
	HP = 1,
	ExpGain = 2,
	ChestOpen = 3,
	QuestFinish = 4,
	PickItem = 5
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TriggerID
{
	None,
	AimEnd,
	AlchemicalBreakOutSkin,
	Appear,
	Attack,
	AutoDefendTrigger,
	Blink,
	BornToClose,
	BornToOpen,
	Break,
	BurstSkinSet,
	Crow_Control_Show,
	Crow_Summon_Show_Aim,
	Crow_Summon_Show_NoAim,
	Crow_Talent_C_ExtreAttack,
	DevilDash_Skin,
	Dice_Attack,
	Dice_Die,
	Die,
	DoSkill,
	ElementalBurst_Skin,
	EndHoldTrigger,
	EndLoopPlusTrigger,
	ExtraAttack_Skin,
	GemStone_Impact,
	GeoOrderSkin_Strong,
	GeoOrderSkin_Weak,
	Heal,
	Hide_Avatar_OnRemoved,
	Idle,
	Mark_1,
	Mark_2,
	Mark_End_1,
	Mark_End_2,
	Mask_TriggerTalent,
	NextLoopTrigger,
	NextSkillTrigger,
	Panda_Attack_Trigger,
	PRIVATE_DoLastSpecialExtra,
	PRIVATE_DoSpecialExtra,
	PRIVATE_ElementalArt_End,
	PRIVATE_ExtraEndCharging,
	PRIVATE_NormalToSneak,
	PRIVATE_SneakToNormal,
	Skill_E_SpecialSkin,
	Skill_E_SpecialToNormalSkin,
	Skin_Strong,
	SkinActive_1,
	SkinActive_2,
	SkinOn,
	SkinReset,
	SkinSet,
	SkinTrigger,
	Sprint_Skin,
	Start,
	StealthOff,
	StealthOn,
	Switch,
	Trigger_Burst,
	Trigger_Hit,
	ToShaderState1,
	ToShaderState2,
	ShiledBrokenTrigger,
	BombThrowTrigger,
	WeakStartTrigger,
	WeakTrigger,
	DefendTrigger,
	AttackLanded,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum PlayMode
{
	GlidingChallengeState,
	FleurFairFall,
	DvalinS01FlyState,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ReactionType
{
	None,
	Burning,
	CrystallizeElectric,
	CrystallizeFire,
	CrystallizeIce,
	CrystallizeWater,
	Explode,
	Freeze,
	Melt,
	Shock,
	Stream,
	Superconductor,
	SwirlElectric,
	SwirlFire,
	SwirlIce,
	SwirlWater,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum TurnMode
{
	OnlyTarget,
	OnlyCamera
}