using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;
using System.Collections.Generic;
using System.Numerics;

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
		public List<AbilityInstance> InstancedAbilities { get; } = new();
		public Dictionary<uint, AbilityModifierController> InstancedModifiers { get; } = new();
		public Dictionary<int, AnimatorParameterValueInfo> AnimatorParameters { get; } = new();
		public EntityRendererChangedInfo? CachedRendererChangedInfo { get; set; }

		// Mirrors hk4e's Entity::motion_state_: the last known motion state
		// as updated from client MotionInfo. Used when serializing MotionInfo
		// in SceneEntityInfo so that MotionInfo.State reflects the entity's
		// actual motion state instead of always MOTION_NONE.
		public MotionState MotionState { get; private set; }

		// Simple shield bar state as reported by ShieldBarMixin.
		// This mirrors the client-side shield bar UI but does not yet
		// participate in damage calculation or GlobalMainShield logic.
		public float ShieldBarCurrent { get; private set; }
		public float ShieldBarMax { get; private set; }
		public uint ShieldBarElementType { get; private set; }

		public BaseAbilityManager? abilityManager = null;
		public uint _EntityId { get; protected set; }
		public Vector3 Position { get; set; }
		public Vector3 Rotation { get; set; }
		public Session session { get; }

		protected ProtEntityType EntityType { get; }

		protected Entity(Session session, Vector3? position, Vector3? rotation, ProtEntityType entityType, uint? entityId = null)
		{
			this.session = session;
			Position = position ?? session.player!.Pos;
			Rotation = rotation ?? session.player!.Rot;
			EntityType = entityType;
			_EntityId = entityId ?? session.GetEntityId(entityType);

			// Initialize motion state to match hk4e spawn behavior:
			// living entities (avatars, monsters, NPCs) start in STANDBY,
			// while non-creatures default to NONE until motion is set.
			MotionState = entityType switch
			{
				ProtEntityType.ProtEntityAvatar => MotionState.MotionStandby,
				ProtEntityType.ProtEntityMonster => MotionState.MotionStandby,
				ProtEntityType.ProtEntityNpc => MotionState.MotionStandby,
				_ => MotionState.MotionNone
			};
		}

		public void UpdateShieldBar(uint elementType, float shield, float maxShield)
		{
			ShieldBarElementType = elementType;
			ShieldBarCurrent = shield;
			ShieldBarMax = maxShield;
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
				State = MotionState
			};
		}

		public void SetMotionState(MotionState state)
		{
			MotionState = state;
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
				AbilityInfo = new()
			};

			BuildKindSpecific(info);
			InjectCommonProps(info);
			foreach (var kv in AnimatorParameters)
				info.AnimatorParaMaps[kv.Key] = kv.Value;
			if (CachedRendererChangedInfo != null)
				info.RendererChangedInfo = CachedRendererChangedInfo;

			return info;
		}

		public virtual void ForceKill()
		{
			if (this is not IDamageable)
				return;

			OnDied(Protocol.VisionType.VisionDie);
		}

		protected virtual void OnDied(Protocol.VisionType disappearType)
		{

			// Default network notifications for entity death.
			session.SendPacket(new LifeStateChangeNotify { EntityId = _EntityId, LifeState = 2 });
			session.SendPacket(new SceneEntityDisappearNotify
			{
				EntityLists = { _EntityId },
				DisappearType = disappearType
			});
			// Prefer going through the owning scene's EntityManager when available,
			// but avoid double-sending disappear packets.
			if (session.player?.Scene != null)
			{
				session.player.Scene.EntityManager.Remove(_EntityId, disappearType, notifyClients: false);
				return;
			}

		}

        public void GenerateElemBallByAbility(GenerateElemBall info, AbilityActionGenerateElemBall generateElemBall)
        {
            // Default to an elementless particle.
            int cfgId = Convert.ToInt32(info.configID);
            int itemId = cfgId != 0 ? cfgId : 2024;

			// Generate 2 particles by default. todo: use info.ratio and info.baseEnergy?
			int amount = info.GetBaseEnergy();

            int gadgetId = MainApp.resourceManager.MaterialExcel.Values.FirstOrDefault(m => m.id == itemId)?.gadgetId is uint gid ? (int)gid : 70610008; // no element by default

            new Logger("GenerateElemBall").LogInfo($"Generating {amount} element balls of item ID {itemId} (gadget ID {gadgetId}) for entity ID {_EntityId}");

            session.player.Scene.GenerateParticles(
                gadgetId,
                amount,
                generateElemBall.Pos,
                generateElemBall.Rot
            );
        }

        private int getBallIdForElement(Resource.ElementType element)
        {
            return element switch
            {
                Resource.ElementType.Fire => 2017,
                Resource.ElementType.Water => 2018,
                Resource.ElementType.Grass => 2019,
                Resource.ElementType.Electric => 2020,
                Resource.ElementType.Wind => 2021,
                Resource.ElementType.Ice => 2022,
                Resource.ElementType.Rock => 2023,
                _ => 2024
            };
        }

        public SceneGroupLua? GetEntityGroup(uint groupId) => session.player!.Scene.GetGroup((int)groupId);

		//public virtual void GenerateElemBall(AbilityActionGenerateElemBall info) { }
	}
}
