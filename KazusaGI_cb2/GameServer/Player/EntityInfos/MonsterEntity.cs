using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;

namespace KazusaGI_cb2.GameServer
{
	public class MonsterEntity : Entity, IDamageable
	{
		public MonsterLua? _monsterInfo;
		public MonsterExcelConfig excelConfig;
		public uint _monsterId;
		public uint level;
		public float Hp { get; private set; }
		public float MaxHp { get; private set; }
		public float Atk { get; private set; }
		public float Def { get; private set; }

		public MonsterEntity(Session session, uint monsterId, MonsterLua? monsterInfo = null, Vector3? position = null, Vector3? rotation = null)
			: base(session, position, rotation, ProtEntityType.ProtEntityMonster)
		{
			_monsterInfo = monsterInfo;
			_monsterId = monsterId;

			level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
			excelConfig = MainApp.resourceManager.MonsterExcel[monsterId];

			Hp = MaxHp = excelConfig.hpBase;
			Atk = excelConfig.attackBase;
			Def = excelConfig.defenseBase;

			ReCalculateFightProps();
		}

		public void ApplyDamage(float amount, AttackResult attack) => Damage(amount);

		protected override uint? GetLevel() => level;

		public void ReCalculateFightProps()
		{
			var curveConfig = MainApp.resourceManager.MonsterCurveExcel[this.level];

			float baseHp = excelConfig.hpBase;
			float baseAtk = excelConfig.attackBase;
			float baseDef = excelConfig.defenseBase;

			baseHp = ApplyCurve(baseHp, curveConfig, FightPropType.FIGHT_PROP_BASE_HP);
			baseAtk = ApplyCurve(baseAtk, curveConfig, FightPropType.FIGHT_PROP_BASE_ATTACK);
			baseDef = ApplyCurve(baseDef, curveConfig, FightPropType.FIGHT_PROP_BASE_DEFENSE);

			MaxHp = Hp = baseHp;
			Atk = baseAtk;
			Def = baseDef;
		}

		private float ApplyCurve(float baseValue, MonsterCurveExcelConfig curveConfig, FightPropType prop)
		{
			var growType = excelConfig.propGrowCurves.Find(c => c.type == prop)!.growCurve;
			var g = curveConfig.curveInfos.Find(c => c.type == growType)!;
			return g.arith switch
			{
				ArithType.ARITH_MULTI => baseValue + baseValue * g.value,
				ArithType.ARITH_ADD => baseValue + g.value,
				ArithType.ARITH_SUB => baseValue - g.value,
				ArithType.ARITH_DIVIDE => baseValue - baseValue / g.value,
				_ => baseValue
			};
		}

		protected override Dictionary<uint, float> GetFightProps()
		{
			var ret = new Dictionary<uint, float>
			{
				[(uint)FightPropType.FIGHT_PROP_BASE_HP] = excelConfig.hpBase,
				[(uint)FightPropType.FIGHT_PROP_BASE_ATTACK] = excelConfig.attackBase,
				[(uint)FightPropType.FIGHT_PROP_BASE_DEFENSE] = excelConfig.defenseBase,
				[(uint)FightPropType.FIGHT_PROP_CUR_ATTACK] = Atk,
				[(uint)FightPropType.FIGHT_PROP_CUR_DEFENSE] = Def,
				[(uint)FightPropType.FIGHT_PROP_CUR_HP] = Hp,
				[(uint)FightPropType.FIGHT_PROP_MAX_HP] = MaxHp
			};
			return ret;
		}

		protected override void BuildKindSpecific(SceneEntityInfo ret)
		{
			var sceneMonsterInfo = new SceneMonsterInfo
			{
				AuthorityPeerId = 1,
				MonsterId = _monsterId,
				IsElite = _monsterInfo?.isElite ?? false,
				ConfigId = _monsterInfo?.config_id ?? 0,
				BornType = MonsterBornType.MonsterBornDefault,
				PoseId = _monsterInfo?.pose_id ?? 0,
				BlockId = _monsterInfo?.block_id ?? 0,
				GroupId = _monsterInfo?.group_id ?? 0
			};

			// Attach weapons (if any)
			if (excelConfig.equips.Count > 0)
			{
				foreach (uint equipId in excelConfig.equips)
				{
					if (equipId == 0) continue;
					var weaponEntity = new WeaponEntity(session, equipId);
					sceneMonsterInfo.WeaponLists.Add(new SceneWeaponInfo
					{
						EntityId = weaponEntity._EntityId,
						GadgetId = equipId,
					});
					session.entityMap[weaponEntity._EntityId] = weaponEntity;
				}
			}

			ret.Monster = sceneMonsterInfo;
		}

		public void Damage(float dmg)
		{
			Hp -= dmg;
			if (Hp < 0) Hp = 0;

			var upd = new EntityFightPropUpdateNotify
			{
				EntityId = _EntityId
			};
			upd.FightPropMaps[(uint)FightPropType.FIGHT_PROP_CUR_HP] = Hp;
			session.SendPacket(upd);

			if (Hp <= 0) Die();
		}

		public void Die(VisionType vision = VisionType.VisionDie)
		{
			Hp = 0;
			session.SendPacket(new LifeStateChangeNotify { EntityId = _EntityId, LifeState = 2 });
			session.SendPacket(new SceneEntityDisappearNotify { EntityLists = { _EntityId }, DisappearType = vision });
			session.entityMap.Remove(_EntityId);

			if (_monsterInfo != null)
			{
				Lua.LuaManager.executeTriggersLua(
					session,
					session.player!.Scene.GetGroup((int)_monsterInfo.group_id)!,
					new Lua.ScriptArgs((int)_monsterInfo.group_id, (int)Lua.TriggerEventType.EVENT_ANY_MONSTER_DIE, (int)_monsterInfo.config_id)
				);
			}
		}
	}
}
