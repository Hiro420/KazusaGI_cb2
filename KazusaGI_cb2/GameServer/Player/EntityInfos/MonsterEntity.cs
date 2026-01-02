using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.ServerExcel;

namespace KazusaGI_cb2.GameServer
{
	public class MonsterEntity : Entity, IDamageable
	{
		public MonsterLua? _monsterInfo { get; }
		public MonsterExcelConfig excelConfig { get; }
		public MonsterRow serverExcelConfig { get; }
		public uint _monsterId { get; }
		public uint level { get; }
		public float Hp { get; private set; }
		public float MaxHp { get; private set; }
		public float Atk { get; private set; }
		public float Def { get; private set; }
		public HashSet<uint> DroppedPercents { get; private set; } = new();

		public MonsterEntity(Session session, uint monsterId, MonsterLua? monsterInfo = null, Vector3? position = null, Vector3? rotation = null)
			: base(session, position, rotation, ProtEntityType.ProtEntityMonster)
		{
			_monsterInfo = monsterInfo;
			_monsterId = monsterId;

			level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
			excelConfig = MainApp.resourceManager.MonsterExcel[monsterId];
			serverExcelConfig = MainApp.resourceManager.ServerMonsterRows.First(row => row.Id == monsterId);

			Hp = MaxHp = excelConfig.hpBase;
			Atk = excelConfig.attackBase;
			Def = excelConfig.defenseBase;

			ReCalculateFightProps();
			abilityManager = new MonsterAbilityManager(this);
			abilityManager.Initialize();
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
			if (!excelConfig.propGrowCurves.Exists(c => c.type == prop))
				return baseValue;
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
				AuthorityPeerId = session.player!.PeerId,
				MonsterId = _monsterId,
				IsElite = _monsterInfo?.isElite ?? false,
				ConfigId = _monsterInfo?.config_id ?? 0,
				BornType = MonsterBornType.MonsterBornDefault,
				PoseId = _monsterInfo?.pose_id ?? 0,
				BlockId = _monsterInfo?.block_id ?? 0,
				GroupId = _monsterInfo?.group_id ?? 0,
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
						Level = 1,
						AbilityInfo = new()
					});
					foreach (var kv in weaponEntity.GetAffixMap())
					{
						sceneMonsterInfo.WeaponLists[^1].AffixMaps[kv.Key] = kv.Value;
					}
					session.player!.Scene.EntityManager.Add(weaponEntity);

					SceneEntityAppearNotify appearNotify = new SceneEntityAppearNotify
					{
						AppearType = Protocol.VisionType.VisionMeet,
						EntityLists = { weaponEntity.ToSceneEntityInfo() }
					};
					session.SendPacket(appearNotify);
				}
			}

			foreach (uint affix in _monsterInfo?.affix ?? new List<uint>())
			{
				sceneMonsterInfo.AffixLists.Add(affix);
			}

			ret.Monster = sceneMonsterInfo;
		}

		public void Damage(float dmg)
		{
			if (dmg <= 0)
				return;

			Hp -= dmg;
			if (Hp < 0)
				Hp = 0;

			CheckForDrop();

			var update = new EntityFightPropUpdateNotify
			{
				EntityId = _EntityId
			};
			update.FightPropMaps[(uint)FightPropType.FIGHT_PROP_CUR_HP] = Hp;
			session.SendPacket(update);

			if (Hp <= 0)
				OnDied(Protocol.VisionType.VisionDie);
		}

		public void Die(Protocol.VisionType vision = Protocol.VisionType.VisionDie)
		{
			Hp = 0;
			OnDied(vision);
		}

		protected override void OnDied(Protocol.VisionType disappearType)
		{
			base.OnDied(disappearType);

			if (_monsterInfo == null)
				return;

			// Notify the scene's challenge system about this kill so that
			// dungeon challenges (e.g., kill count / time between kills)
			// can update their internal state.
			session.player!.Scene.OnMonsterDie(_monsterInfo.group_id, _monsterInfo.config_id);

			Lua.LuaManager.executeTriggersLua(
				session,
				session.player!.Scene.GetGroup((int)_monsterInfo.group_id)!,
				new Lua.ScriptArgs(
					(int)_monsterInfo.group_id,
					(int)Lua.EventType.EVENT_ANY_MONSTER_DIE,
					(int)_monsterInfo.config_id));
		}

		private void CheckForDrop()
		{
			foreach (var hpDrop in excelConfig.hpDrops)
			{
				if (Hp / MaxHp <= hpDrop.hpPercent / 100f && !DroppedPercents.Contains(hpDrop.hpPercent))
				{
					DropManager.DropLoot(session, hpDrop.dropId, this);
					DroppedPercents.Add(hpDrop.hpPercent);
				}
			}
			if (Hp <= 0)
			{
				DropManager.DropLoot(session, excelConfig.killDropId, this);
			}
		}
	}
}
