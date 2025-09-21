using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Protocol;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.GameServer.Ability;

namespace KazusaGI_cb2.GameServer
{
	public interface IDamageable
	{
		float Hp { get; }
		float MaxHp { get; }
		void ApplyDamage(float amount, AttackResult attack);
	}

	public abstract class Entity
	{
		public BaseAbilityManager? abilityManager = null;
		public uint _EntityId { get; protected set; }
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }
		public Session session { get; }

		protected ProtEntityType EntityType { get; }

		protected Entity(Session session, Vector3? position, Vector3? rotation, ProtEntityType entityType, uint? entityId = null)
		{
			this.session = session;
			this.Position = position ?? session.player!.Pos;
			this.Rotation = rotation ?? session.player!.Rot;
			this.EntityType = entityType;

			this._EntityId = entityId ?? session.GetEntityId(entityType);
		}

		protected virtual uint? GetLevel() => null;

		protected virtual uint GetLifeState()
		{
			if (this is IDamageable dmg)
				return dmg.Hp > 0 ? 1u : 0u;
			return 1u;
		}

		protected virtual Dictionary<uint, float> GetFightProps() => new();

		protected abstract void BuildKindSpecific(SceneEntityInfo info);

		protected (Protocol.Vector pos, Protocol.Vector rot) ResolvePosRot(Vector3? luaPos, Vector3? luaRot)
		{
			var pos = Session.Vector3ToVector(luaPos ?? this.Position);
			var rot = Session.Vector3ToVector(luaRot ?? this.Rotation);
			return (pos, rot);
		}

		protected MotionInfo MakeMotion(Vector3? luaPos, Vector3? luaRot)
		{
			var (pos, rot) = ResolvePosRot(luaPos, luaRot);
			return new MotionInfo
			{
				Pos = pos,
				Rot = rot,
				Speed = new Protocol.Vector(),
				State = MotionState.MotionNone
			};
		}

		protected SceneEntityAiInfo MakeAi(Vector3? bornAt)
		{
			var born = Session.Vector3ToVector(bornAt ?? this.Position);
			return new SceneEntityAiInfo { IsAiOpen = true, BornPos = born };
		}

		protected void InjectCommonProps(SceneEntityInfo info)
		{
			info.LifeState = GetLifeState();

			var lvl = GetLevel();
			if (lvl.HasValue)
			{
				info.PropMaps[(uint)PropType.PROP_LEVEL] = new PropValue
				{
					Type = (uint)PropType.PROP_LEVEL,
					Ival = lvl.Value,
					Val = lvl.Value
				};
			}

			foreach (var kv in GetFightProps())
				info.FightPropMaps[kv.Key] = kv.Value;
		}

		public SceneEntityInfo ToSceneEntityInfo(Vector3? luaPos = null, Vector3? luaRot = null)
		{
			var info = new SceneEntityInfo
			{
				EntityType = EntityType,
				EntityId = this._EntityId,
				MotionInfo = MakeMotion(luaPos, luaRot),
				AiInfo = MakeAi(luaPos),
				AbilityInfo = GetAbilityStates()
			};

			BuildKindSpecific(info);
			InjectCommonProps(info);
			return info;
		}

		public AbilitySyncStateInfo GetAbilityStates()
		{
			if (abilityManager == null)
				return new();
			AbilitySyncStateInfo ret = new AbilitySyncStateInfo()
			{
				IsInited = true // todo: acutally check
			};
		
			if (abilityManager.InstanceToAbilityHashMap != null)
			{
				foreach (var appliedAbility in abilityManager.InstanceToAbilityHashMap.Values)
				{
					AbilityAppliedAbility proto = new AbilityAppliedAbility()
					{
						AbilityName = new AbilityString()
						{
							Hash = appliedAbility,
							Str = abilityManager.ConfigAbilityHashMap?.GetValueOrDefault(appliedAbility)?.abilityName,
						},
					};
					ret.AppliedAbilities.Add(proto);
				}
			}
		
			if (abilityManager.GlobalValueHashMap != null)
			{
				foreach (var dynamicValue in abilityManager.GlobalValueHashMap)
				{
					ret.DynamicValueMaps.Add(dynamicValue.Value);
				}
			}

			return ret;
		}

		public virtual void ForceKill()
		{
			if (this is IDamageable damageable)
			{
				// Force HP to 0 and notify of HP change
				var hpField = damageable.GetType().GetProperty("Hp");
				if (hpField != null && hpField.CanWrite)
				{
					hpField.SetValue(damageable, 0f);
				}

				// Send HP update notification
				var upd = new EntityFightPropUpdateNotify
				{
					EntityId = _EntityId
				};
				upd.FightPropMaps[(uint)Resource.FightPropType.FIGHT_PROP_CUR_HP] = 0f;
				session.SendPacket(upd);

				// Send life state change notification (dead)
				session.SendPacket(new LifeStateChangeNotify { EntityId = _EntityId, LifeState = 2 });

				// Send entity disappear notification
				session.SendPacket(new SceneEntityDisappearNotify { EntityLists = { _EntityId }, DisappearType = Protocol.VisionType.VisionDie });

				// Remove from entity map
				session.entityMap.Remove(_EntityId);

				// If this is a MonsterEntity, trigger lua events
				if (this is MonsterEntity monster && monster._monsterInfo != null)
				{
					Lua.LuaManager.executeTriggersLua(
						session,
						session.player!.Scene.GetGroup((int)monster._monsterInfo.group_id)!,
						new Lua.ScriptArgs((int)monster._monsterInfo.group_id, (int)Lua.TriggerEventType.EVENT_ANY_MONSTER_DIE, (int)monster._monsterInfo.config_id)
					);
				}
			}
		}

		public SceneGroupLua? GetEntityGroup(uint groupId) => session.player!.Scene.GetGroup((int)groupId);

		public virtual void GenerateElemBall(AbilityActionGenerateElemBall info) { }
	}
}
