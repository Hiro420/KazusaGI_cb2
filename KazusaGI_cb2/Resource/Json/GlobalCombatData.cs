using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Json;

public class GlobalCombatData
{
	public LogSetting logSetting { get; set; }
	public DieData dieData { get; set; }
	public HitData hitData { get; set; }
	public CollisionData collisionData { get; set; }
	public AiData aiData { get; set; }
	public MoveData moveData { get; set; }
	public ElementBladeData elementBladeData { get; set; }
	public FireGrassAirflowData fireGrassAirflowData { get; set; }
	public Miscs miscs { get; set; }
	public LockTarget lockTarget { get; set; }
	public AttackAttenuation attackAttenuation { get; set; }
	public DefaultAbilities defaultAbilities { get; set; }
	public ElemReactDamage elemReactDamage { get; set; }
	public ElemAmplifyDamage elemAmplifyDamage { get; set; }
	public List<string> elemPrority { get; set; }
	public ShakeOff shakeOff { get; set; }
	public ElementShield elementShield { get; set; }
	public EliteShield eliteShield { get; set; }
	public GlobalSwitch globalSwitch { get; set; }
	public ElemUi elemUI { get; set; }
	public RejectElementReaction rejectElementReaction { get; set; }
	public LuaSafetySwitch luaSafetySwitch { get; set; }
	public List<GadgetCreationLimit> gadgetCreationLimits { get; set; }
	public GlobalInteraction globalInteraction { get; set; }
	public List<LampOffset> lampOffset { get; set; }
	public Dictionary<string, double> tempComponentBudget { get; set; }
}

public partial class AiData
{
	public string type { get; set; }
	public long avatarMeleeSlotRadius { get; set; }
	public double facingMoveTurnInterval { get; set; }
	public double facingMoveMinAvoidanceVelecity { get; set; }
	public double avoidanceUpdateInterval { get; set; }
	public long avoidanceRadius { get; set; }
	public long avoidanceForce { get; set; }
	public long lod0 { get; set; }
	public long lod1 { get; set; }
	public long lod2 { get; set; }
	public PublicCDs publicCDs { get; set; }
	public DefaultWeatherNeuronMapping defaultWeatherNeuronMapping { get; set; }
	public bool useServerPathfinding { get; set; }
}

public partial class DefaultWeatherNeuronMapping
{
	public List<string> Rain { get; set; }
	public List<string> Storm { get; set; }
}

public partial class PublicCDs
{
	public RandomAction meleeAttack_long { get; set; }
	public RandomAction rangedAttack_long { get; set; }
	public RandomAction RandomAction { get; set; }
}

public partial class RandomAction
{
	public string type { get; set; }
	public string name { get; set; }
	public double minInterval { get; set; }
}

public partial class AttackAttenuation
{
	public long resetCycle { get; set; }
	public List<long> durabilitySequence { get; set; }
	public List<double> enbreakSequence { get; set; }
}

public partial class CollisionData
{
	public string type { get; set; }
	public long highSpdThreshold { get; set; }
}

public partial class DefaultAbilities
{
	public string monterEliteAbilityName { get; set; }
	public List<string> nonHumanoidMoveAbilities { get; set; }
	public List<string> levelDefaultAbilities { get; set; }
	public List<string> levelElementAbilities { get; set; }
	public List<string> levelItemAbilities { get; set; }
	public List<string> levelSBuffAbilities { get; set; }
	public List<object> dungeonAbilities { get; set; }
	public List<string> defaultTeamAbilities { get; set; }
	public List<string> defaultMPLevelAbilities { get; set; }
	public List<string> defaultAvatarAbilities { get; set; }
}

public partial class DieData
{
	public string type { get; set; }
	public long dieEndTime { get; set; }
	public long dieEndMaxTime { get; set; }
}

public partial class ElemAmplifyDamage
{
	public Stream Stream { get; set; }
	public Melt Melt { get; set; }
}

public partial class Melt
{
	public long Fire { get; set; }
	public double Ice { get; set; }
}

public partial class Stream
{
	public long Water { get; set; }
	public double Fire { get; set; }
}

public partial class ElemReactDamage
{
	public Burning Burning { get; set; }
	public Burning Stream { get; set; }
	public Burning Explode { get; set; }
	public Burning GrassGrowing { get; set; }
	public Burning Shock { get; set; }
	public Burning Freeze { get; set; }
	public Burning Melt { get; set; }
}

public partial class Burning
{
}

public partial class ElemUi
{
	public List<string> showIconEntityTypes { get; set; }
	public List<string> showReactionEntityTypes { get; set; }
	public double iconRecoverTime { get; set; }
	public long iconDisappearTime { get; set; }
	public long iconDisappearRound { get; set; }
	public long iconShowDistance { get; set; }
	public OverrideElemPath overrideElemPath { get; set; }
	public ReactionElemPath reactionElemPath { get; set; }
}

public partial class OverrideElemPath
{
	public string Burning { get; set; }
	public string Frozen { get; set; }
}

public partial class ReactionElemPath
{
	public string Burning { get; set; }
	public string Explode { get; set; }
	public string Freeze { get; set; }
	public string Melt { get; set; }
	public string Shock { get; set; }
	public string Stream { get; set; }
	public string Superconductor { get; set; }
	public string SwirlElectric { get; set; }
	public string SwirlFire { get; set; }
	public string SwirlIce { get; set; }
	public string SwirlWater { get; set; }
}

public partial class ElementBladeData
{
	public ElementBladeDataElectric Fire { get; set; }
	public ElementBladeDataElectric Water { get; set; }
	public ElementBladeDataElectric Wind { get; set; }
	public ElementBladeDataElectric Ice { get; set; }
	public ElementBladeDataElectric Electric { get; set; }
}

public partial class ElementBladeDataElectric
{
	public string slash { get; set; }
	public string colorA { get; set; }
	public string colorB { get; set; }
}

public partial class ElementShield
{
	public List<string> row { get; set; }
	public ElementShieldShieldDamageRatiosMap shieldDamageRatiosMap { get; set; }
}

public partial class ElementShieldShieldDamageRatiosMap
{
	public FrozenClass None { get; set; }
	public FrozenClass Fire { get; set; }
	public FrozenClass Water { get; set; }
	public FrozenClass Grass { get; set; }
	public FrozenClass Ice { get; set; }
	public FrozenClass Frozen { get; set; }
	public FrozenClass Electric { get; set; }
	public FrozenClass Wind { get; set; }
	public FrozenClass Rock { get; set; }
}

public partial class FrozenClass
{
	public string elementType { get; set; }
	public List<long> damageRatio { get; set; }
	public List<long> restraint { get; set; }
}

public partial class EliteShield
{
	public List<string> row { get; set; }
	public EliteShieldShieldDamageRatiosMap shieldDamageRatiosMap { get; set; }
}

public partial class EliteShieldShieldDamageRatiosMap
{
	public AvatarElectric @default { get; set; }
	public Xingqiu xingqiu { get; set; }
	public AvatarElectric Avatar_Fire { get; set; }
	public AvatarElectric Avatar_Water { get; set; }
	public AvatarElectric Avatar_Grass { get; set; }
	public AvatarElectric Avatar_Ice { get; set; }
	public AvatarElectric Avatar_Frozen { get; set; }
	public AvatarElectric Avatar_Electric { get; set; }
	public AvatarElectric Avatar_Wind { get; set; }
	public AvatarElectric Avatar_Rock { get; set; }
}

public partial class AvatarElectric
{
	public string type { get; set; }
	public List<double> damageRatio { get; set; }
}

public partial class Xingqiu
{
	public string type { get; set; }
	public List<long> damageRatio { get; set; }
	public List<double> damageSufferRatio { get; set; }
}

public partial class FireGrassAirflowData
{
	public string type { get; set; }
	public bool enable { get; set; }
	public long gadgetId { get; set; }
	public long triggerNum { get; set; }
	public long height { get; set; }
	public bool up { get; set; }
	public long velocity { get; set; }
	public long heightSpeed { get; set; }
	public double antiGravityRatio { get; set; }
}

public partial class GadgetCreationLimit
{
	public string tag { get; set; }
	public List<long> gadgetIDs { get; set; }
	public long maxValue { get; set; }
}

public partial class GlobalInteraction
{
	public long talkEnableAngle { get; set; }
}

public partial class GlobalSwitch
{
	public bool enableMultiPlayer { get; set; }
	public bool enableAnimatorInterleave { get; set; }
	public bool enableMixinModifierDetachCallback { get; set; }
}

public partial class HitData
{
	public string type { get; set; }
	public long minHitVX { get; set; }
	public long maxHitVX { get; set; }
	public long minHitVY { get; set; }
	public long maxHitVY { get; set; }
	public long gravity { get; set; }
	public long hitRetreatFriction { get; set; }
	public double airFrictionX { get; set; }
	public long airFrictionY { get; set; }
	public double dieRetreatRatio { get; set; }
	public long dieRetreatAdd { get; set; }
	public long dieRetreatAirXAdd { get; set; }
	public long dieRetreatAirYAdd { get; set; }
}

public partial class LampOffset
{
	public double x { get; set; }
	public double y { get; set; }
	public double z { get; set; }
}

public partial class LockTarget
{
	public long lockWeightOutCameraParam { get; set; }
	public double lockWeightRelockParam { get; set; }
	public long clearLockTargetOutCombat { get; set; }
	public long clearLockTargetInCombat { get; set; }
	public long forceLockTargetInCombat { get; set; }
}

public partial class LogSetting
{
	public bool sendEngineLogToServer { get; set; }
}

public partial class LuaSafetySwitch
{
	public bool tickWorld { get; set; }
	public long tickWorldCD { get; set; }
	public bool tickChest { get; set; }
	public bool tickMonster { get; set; }
}

public partial class Miscs
{
	public double airFlowAcc { get; set; }
	public long paimonGadgetID { get; set; }
	public string cureEffect { get; set; }
	public string gadgetUICameraLookCfgPath { get; set; }
	public string gadgetUICutSenceCfgPath { get; set; }
	public string weaponAnimCurvePath { get; set; }
	public AvatarFocus avatarFocus { get; set; }
}

public partial class AvatarFocus
{
	public Other ps4 { get; set; }
	public Other other { get; set; }
	public Other pc { get; set; }
}

public partial class Other
{
	public double cameraHorMoveSpeed { get; set; }
	public double cameraVerMoveSpeed { get; set; }
	public double cameraHorStickyRatio { get; set; }
	public double cameraVerStickyRatio { get; set; }
	public long autoFocusHorSpeed { get; set; }
	public long autoFocusVerSpeed { get; set; }
	public double autoFocusRangeCoef { get; set; }
	public long gyroHorMoveSpeed { get; set; }
	public double gyroVerMoveSpeed { get; set; }
}

public partial class MoveData
{
	public bool noGroundStayInPlace { get; set; }
	public SyncInterval syncInterval { get; set; }
}

public partial class SyncInterval
{
	public Lod lod0 { get; set; }
	public Lod lod1 { get; set; }
	public Lod2 lod2 { get; set; }
}

public partial class Lod
{
	public double defaultValue { get; set; }
	public SpecificValue specificValue { get; set; }
}

public partial class SpecificValue
{
	public double EnvAnimal { get; set; }
}

public partial class Lod2
{
	public long defaultValue { get; set; }
}

public partial class RejectElementReaction
{
	public List<string> ElementFreeze { get; set; }
	public List<string> ElementFrozen { get; set; }
}

public partial class ShakeOff
{
	public long reduceDurability { get; set; }
	public double interval { get; set; }
	public double shakeLifeTime { get; set; }
	public double shakeValue { get; set; }
}