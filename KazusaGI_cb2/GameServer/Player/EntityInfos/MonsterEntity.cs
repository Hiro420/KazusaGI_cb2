using KazusaGI_cb2.GameServer.PlayerInfos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource;
using NLua;
using System.Resources;
using KazusaGI_cb2.GameServer.Lua;
using static KazusaGI_cb2.Utils.ENet;
using System.Text.RegularExpressions;

namespace KazusaGI_cb2.GameServer;

public class MonsterEntity : Entity
{
    public MonsterLua? _monsterInfo;
    public MonsterExcelConfig excelConfig;
    public uint _monsterId;
    public uint level;
    public float Hp;
    public float MaxHp;
    public float Atk;
    public float Def;

    public MonsterEntity(Session session, uint MonsterId, MonsterLua? monsterInfo, Vector3? position)
        : base(session, position)
    {
        this._EntityId = session.GetEntityId(ProtEntityType.ProtEntityMonster);
        _monsterInfo = monsterInfo;
        _monsterId = MonsterId;
        this.level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
                    // monsterInfo != null ? monsterInfo.level : MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
        this.excelConfig = MainApp.resourceManager.MonsterExcel[MonsterId];
        this.Hp = excelConfig.hpBase;
        this.MaxHp = this.Hp;
        this.Atk = excelConfig.attackBase;
        this.Def = excelConfig.defenseBase;
        this.ReCalculateFightProps();
    }

    public void ReCalculateFightProps()
    {
        // needed for the avatar curve calculations
        MonsterCurveExcelConfig curveConfig = MainApp.resourceManager.MonsterCurveExcel[this.level];

        // init
        float baseHp = this.excelConfig.hpBase;
        float baseAtk = this.excelConfig.attackBase;
        float baseDef = this.excelConfig.defenseBase;

        // start with HP
        // first we do character curves
        GrowCurveType growCurveType = this.excelConfig.propGrowCurves.Find(c => c.type == FightPropType.FIGHT_PROP_BASE_HP)!.growCurve;
        GrowCurveInfo growCurve_Avatar_hp = curveConfig.curveInfos.Find(c => c.type == growCurveType)!;
        baseHp = CalculateByArith(baseHp, growCurve_Avatar_hp.value, growCurve_Avatar_hp.arith);

        // then we do Atk
        growCurveType = this.excelConfig.propGrowCurves.Find(c => c.type == FightPropType.FIGHT_PROP_BASE_ATTACK)!.growCurve;
        GrowCurveInfo growCurve_Avatar_atk = curveConfig.curveInfos.Find(c => c.type == growCurveType)!;
        baseAtk = CalculateByArith(baseAtk, growCurve_Avatar_atk.value, growCurve_Avatar_atk.arith);

        // then we do Def
        growCurveType = this.excelConfig.propGrowCurves.Find(c => c.type == FightPropType.FIGHT_PROP_BASE_DEFENSE)!.growCurve;
        GrowCurveInfo growCurve_Avatar_def = curveConfig.curveInfos.Find(c => c.type == growCurveType)!;
        baseDef = CalculateByArith(baseDef, growCurve_Avatar_def.value, growCurve_Avatar_def.arith);

        // set the values
        this.MaxHp = baseHp;
        this.Atk = baseAtk;
        this.Def = baseDef;
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

    public SceneEntityInfo ToSceneEntityInfo()
    {
        SceneEntityInfo ret = new SceneEntityInfo()
        {
            EntityType = ProtEntityType.ProtEntityMonster,
            EntityId = this._EntityId,
            MotionInfo = new MotionInfo()
            {
                Pos = _monsterInfo != null
                    ? Session.Vector3ToVector(_monsterInfo.pos) : Session.Vector3ToVector(this.Position),
                Rot = _monsterInfo != null
                    ? Session.Vector3ToVector(_monsterInfo.rot) : Session.Vector3ToVector(this.Position),
                Speed = new Protocol.Vector(),
                State = MotionState.MotionNone
            },
            LifeState = this.Hp > 0 ? (uint)1 : 0,
            AiInfo = new SceneEntityAiInfo()
            {
                IsAiOpen = true,
                BornPos = _monsterInfo != null
                    ? Session.Vector3ToVector(_monsterInfo.pos) : Session.Vector3ToVector(this.Position),
            },
        };
        SceneMonsterInfo sceneMonsterInfo = new SceneMonsterInfo()
        {
            AuthorityPeerId = 1,
            MonsterId = _monsterId,
            IsElite = _monsterInfo != null ? _monsterInfo.isElite : false,
            ConfigId = _monsterInfo != null ? _monsterInfo.config_id : 0,
            BornType = MonsterBornType.MonsterBornDefault,
            PoseId = _monsterInfo != null ? _monsterInfo.pose_id : 0,
            BlockId = _monsterInfo != null ? _monsterInfo.block_id : 0,
            GroupId = _monsterInfo != null ? _monsterInfo.group_id : 0,
        };
        ret.PropMaps.Add((uint)PropType.PROP_LEVEL, new PropValue() { Type = (uint)PropType.PROP_LEVEL, Ival = this.level, Val = this.level });
        // ret.PropMaps.Add((uint)PropType.PROP_EXP, new PropValue() { Type = (uint)PropType.PROP_EXP, Ival = 1, Val = this.level });
        foreach (uint affixId in this.excelConfig.affix)
        {
            if (affixId == 0 && (this._monsterInfo != null && !this._monsterInfo.affix.Contains(affixId)))
            {
                continue;
            }
        }
        foreach (var prop in this.GetFightProps())
        {
            ret.FightPropMaps.Add(prop.Key, prop.Value);
        }
        if (this.excelConfig.equips.Count > 0)
        {
            foreach(uint equipId in this.excelConfig.equips)
            {
                if (equipId == 0)
                    continue;
                WeaponEntity weaponEntity = new WeaponEntity(session, equipId);
                SceneWeaponInfo sceneWeaponInfo = new SceneWeaponInfo()
                {
                    EntityId = weaponEntity._EntityId,
                    GadgetId = equipId,
                    // Guid = session.GetGuid(),
                    // Level = this.level,
                    // ItemId = equipId,
                };
                sceneMonsterInfo.WeaponLists.Add(sceneWeaponInfo);
                session.entityMap.Add(sceneWeaponInfo.EntityId, weaponEntity);
            }
        }
        ret.Monster = sceneMonsterInfo;
        return ret;
    }

    public void Damage(float dmg)
    {
        this.Hp -= dmg;
        session.c.LogWarning($"Monster {this._EntityId} damaged by {dmg}, current hp: {this.Hp}");
        bool isDie = false;
        if (this.Hp <= 0)
        {
            isDie = true;
            this.Hp = 0;
        }
        EntityFightPropUpdateNotify entityFightPropUpdateNotify = new EntityFightPropUpdateNotify()
        {
            EntityId = this._EntityId
        };
        entityFightPropUpdateNotify.FightPropMaps.Add((uint)FightPropType.FIGHT_PROP_CUR_HP, this.Hp);
        this.session!.SendPacket(entityFightPropUpdateNotify);
        if (isDie)
        {
            this.Die();
        }
    }

    public void Die(VisionType vision = VisionType.VisionDie)
    {
        this.Hp = 0; // for safety
        this.session!.SendPacket(new LifeStateChangeNotify()
        {
            EntityId = this._EntityId,
            LifeState = 2,
            // DieType = PlayerDieType.PlayerDieNone,
        });
        this.session!.SendPacket(new SceneEntityDisappearNotify()
        {
            EntityLists = { this._EntityId },
            DisappearType = vision
        });
        this.session!.entityMap.Remove(this._EntityId);

        if (this._monsterInfo != null)
        {
            LuaManager.executeTriggersLua(
                session,
                session.player!.Scene.GetGroup((int)this._monsterInfo!.group_id)!,
                new ScriptArgs((int)this._monsterInfo!.group_id, (int)TriggerEventType.EVENT_ANY_MONSTER_DIE, (int)this._monsterInfo.config_id)
            );
        };
    }

    
    private Dictionary<uint, float> GetFightProps()
    {
        Dictionary<uint, float> ret = new Dictionary<uint, float>();
        foreach (FightPropType propType in Enum.GetValues(typeof(FightPropType)))
        {
            switch (propType)
            {
                case FightPropType.FIGHT_PROP_BASE_HP:
                    ret.Add((uint)propType, this.excelConfig.hpBase);
                    break;
                case FightPropType.FIGHT_PROP_CUR_ATTACK:
                    ret.Add((uint)propType, this.Atk);
                    break;
                case FightPropType.FIGHT_PROP_BASE_ATTACK:
                    ret.Add((uint)propType, this.excelConfig.attackBase);
                    break;
                case FightPropType.FIGHT_PROP_CUR_DEFENSE:
                    ret.Add((uint)propType, this.Def);
                    break;
                case FightPropType.FIGHT_PROP_BASE_DEFENSE:
                    ret.Add((uint)propType, this.excelConfig.defenseBase);
                    break;
                case FightPropType.FIGHT_PROP_CUR_HP:
                    ret.Add((uint)propType, this.Hp);
                    break;
                case FightPropType.FIGHT_PROP_MAX_HP:
                    ret.Add((uint)propType, this.MaxHp);
                    break;
				case FightPropType.FIGHT_PROP_CUR_SPEED:
				case FightPropType.FIGHT_PROP_CRITICAL:
				case FightPropType.FIGHT_PROP_ANTI_CRITICAL:
				case FightPropType.FIGHT_PROP_CRITICAL_HURT:
				case FightPropType.FIGHT_PROP_CHARGE_EFFICIENCY:
				case FightPropType.FIGHT_PROP_ELEMENT_MASTERY:
				case FightPropType.FIGHT_PROP_CUR_FIRE_ENERGY:
                case FightPropType.FIGHT_PROP_CUR_ELEC_ENERGY:
                case FightPropType.FIGHT_PROP_CUR_WATER_ENERGY:
                case FightPropType.FIGHT_PROP_CUR_GRASS_ENERGY:
                case FightPropType.FIGHT_PROP_CUR_WIND_ENERGY:
                case FightPropType.FIGHT_PROP_CUR_ICE_ENERGY:
                case FightPropType.FIGHT_PROP_CUR_ROCK_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_FIRE_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_ELEC_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_WATER_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_GRASS_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_WIND_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_ICE_ENERGY:
                case FightPropType.FIGHT_PROP_MAX_ROCK_ENERGY:
                    ret.Add((uint)propType, 0.0f);
                    break;
                default:
                    break;
            }
        }
        return ret;
    }
}