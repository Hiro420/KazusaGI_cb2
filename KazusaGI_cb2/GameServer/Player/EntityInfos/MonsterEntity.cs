using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;

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
			abilityManager = new MonsterAbilityManager(this);
			// Instance abilities from monster config and affixes before Initialize so InstanceToAbilityHashMap is populated
			// Affix and default abilities
			// Try to replicate Grasscutter ordering: preAdd affixes -> default non-humanoid -> config abilities -> elite -> postAdd affixes -> level config abilities
			// Pre-add affixes
			ResourceManager resourceManager = MainApp.resourceManager;
			List<uint>? affixes = null;
			// group affixes
			// NOTE: session.player may be null during monster creation in some paths; try to retrieve group config safely
			try
			{
				var optionalGroup = session.player?.Scene?.GetGroup((int)(_monsterInfo?.group_id ?? 0));
				// if available, collect affixes
				// we will fallback to _monsterInfo data below
			}
			catch { }

			if (_monsterInfo != null)
			{
				affixes = _monsterInfo.affix;
			}

			if (affixes != null)
			{
				foreach (var affixId in affixes)
				{
					// Pre-add handling: in Grasscutter some affixes are preAdd; we'll conservatively add all here
					if (resourceManager.MonsterAffixExcel.TryGetValue(affixId, out var affixCfg))
					{
						if (!string.IsNullOrEmpty(affixCfg.abilityName))
						{
							if (!MainApp.resourceManager.ConfigAbilityMap.TryGetValue(affixCfg.abilityName, out var container))
								continue;
							if (container?.Default is ConfigAbility cfg)
								abilityManager.AddAbilityToEntity(this, cfg);
						}
					}
				}
			}

			// Add default abilities from GlobalCombatData so monsters have baseline abilities
			try
			{
				var defaults = resourceManager.GlobalCombatData?.defaultAbilities;
				if (defaults != null)
				{
					// Default non-humanoid move abilities
					if (defaults.nonHumanoidMoveAbilities != null)
					{
						foreach (var name in defaults.nonHumanoidMoveAbilities)
						{
							if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(name, out var cont) && cont.Default is ConfigAbility cfg)
								abilityManager.AddAbilityToEntity(this, cfg);
						}
					}

					// Elite monster default ability (if this monster is elite)
					if ((_monsterInfo?.isElite ?? false) && !string.IsNullOrEmpty(defaults.monterEliteAbilityName))
					{
						var name = defaults.monterEliteAbilityName;
						if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(name, out var cont) && cont.Default is ConfigAbility cfg)
							abilityManager.AddAbilityToEntity(this, cfg);
					}

					// Level-based default abilities
					if (defaults.levelDefaultAbilities != null)
					{
						foreach (var name in defaults.levelDefaultAbilities)
						{
							if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(name, out var cont) && cont.Default is ConfigAbility cfg)
								abilityManager.AddAbilityToEntity(this, cfg);
						}
					}
				}
			}
			catch { }

			// Config entity monster abilities
			if (excelConfig != null && !string.IsNullOrEmpty(excelConfig.monsterName))
			{
				// Monster config abilities are stored in BinOutput; try to pull them
				// If the mapping exists in resource manager, iterate
				string name2search = $"ConfigMonster_{excelConfig.monsterName}";
				if (MainApp.resourceManager.ConfigMonsterMap.TryGetValue(name2search, out var configMonster))
				{
					if (configMonster.abilities != null)
					{
						foreach (var abil in configMonster.abilities)
						{
							if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abil.abilityName, out var cont) && cont.Default is ConfigAbility cfg)
								abilityManager.AddAbilityToEntity(this, cfg);
						}
					}
				}
			}

			// Level entity based abilities
			if (MainApp.resourceManager.ConfigLevelEntityMap.TryGetValue(session.player.SceneId, out var lvlCfg))
			{
				if (lvlCfg.monsterAbilities != null && lvlCfg.monsterAbilities.Count > 0)
				{
					foreach (var mb in lvlCfg.monsterAbilities)
					{
						if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(mb.abilityName, out var cont) && cont.Default is ConfigAbility cfg)
							abilityManager.AddAbilityToEntity(this, cfg);
					}
				}
			}

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

		public void Die(Protocol.VisionType vision = Protocol.VisionType.VisionDie)
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
