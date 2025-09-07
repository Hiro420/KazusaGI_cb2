using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Lua;

namespace KazusaGI_cb2.GameServer
{
	public class GadgetEntity : Entity, IDamageable
	{
		public GadgetLua? _gadgetLua;
		public uint _gadgetId;
		public GadgetExcelConfig gadgetExcel;
		public uint level;

		public float Hp { get; private set; } = 1f;
		public float MaxHp { get; private set; } = 1f;

		public GadgetState state => _gadgetLua?.state ?? GadgetState.Default;

		public GadgetEntity(Session session, uint gadgetId, GadgetLua? gadgetInfo, Vector3? position, Vector3? rotation, uint? entityId = null)
		: base(session, position, rotation, ProtEntityType.ProtEntityGadget, entityId)
		{
			_gadgetId = gadgetId;
			_gadgetLua = gadgetInfo;
			level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
			gadgetExcel = MainApp.resourceManager.GadgetExcel[gadgetId];
		}

		protected override uint? GetLevel() => level;

		protected override Dictionary<uint, float> GetFightProps()
		{
			var ret = new Dictionary<uint, float>
			{
				[(uint)FightPropType.FIGHT_PROP_BASE_HP] = 1f,
				[(uint)FightPropType.FIGHT_PROP_CUR_HP] = Hp,
				[(uint)FightPropType.FIGHT_PROP_MAX_HP] = MaxHp
			};
			return ret;
		}

		protected override void BuildKindSpecific(SceneEntityInfo ret)
		{
			ret.Name = gadgetExcel.jsonName;

			var info = new SceneGadgetInfo
			{
				AuthorityPeerId = 1,
				GadgetState = (uint)state,
				IsEnableInteract = state == GadgetState.Default,
				ConfigId = _gadgetLua?.config_id ?? 0,
				GroupId = _gadgetLua?.group_id ?? 0,
				GadgetId = _gadgetId,
				BornType = GadgetBornType.GadgetBornGadget,
				GadgetType = (uint)(_gadgetLua?.type ?? 0)
			};
			ret.Gadget = info;
		}

		public void ApplyDamage(float amount, AttackResult attack)
		{
			// TODO: Handle by abilities
			//Hp = MathF.Max(0f, Hp - amount);

			//var upd = new EntityFightPropUpdateNotify { EntityId = _EntityId };
			//upd.FightPropMaps[(uint)FightPropType.FIGHT_PROP_CUR_HP] = Hp;
			//session.SendPacket(upd);

			//if (Hp <= 0f)
			//{
			//	session.SendPacket(new LifeStateChangeNotify { EntityId = _EntityId, LifeState = 2 });
			//	session.SendPacket(new SceneEntityDisappearNotify { EntityLists = { _EntityId }, DisappearType = VisionType.VisionDie });
			//	session.entityMap.Remove(_EntityId);
			//}
		}

		public void ChangeState(GadgetState newState)
		{
			if (_gadgetLua == null) return;

			var old = _gadgetLua.state;
			_gadgetLua.state = newState;

			session.SendPacket(new GadgetStateNotify
			{
				GadgetEntityId = _EntityId,
				GadgetState = (uint)newState,
				IsEnableInteract = gadgetExcel.isInteractive
			});

			var args = new ScriptArgs((int)_gadgetLua.group_id, (int)TriggerEventType.EVENT_GADGET_STATE_CHANGE, (int)this.state, (int)_gadgetLua.config_id)
			{
				param3 = (int)old
			};
			LuaManager.executeTriggersLua(session, GetEntityGroup(_gadgetLua.group_id)!, args);
		}
	}
}
