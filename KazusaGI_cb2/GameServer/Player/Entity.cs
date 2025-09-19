using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Protocol;
using System.Collections.Generic;
using System.Numerics;
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
				AbilityInfo = new AbilitySyncStateInfo()
			};

			BuildKindSpecific(info);
			InjectCommonProps(info);
			return info;
		}

		public SceneGroupLua? GetEntityGroup(uint groupId) => session.player!.Scene.GetGroup((int)groupId);

		public virtual void GenerateElemBall(AbilityActionGenerateElemBall info) { }
	}
}
