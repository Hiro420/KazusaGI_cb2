using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System.Linq;
using KazusaGI_cb2.Resource.Json.Avatar;
using Newtonsoft.Json;

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
		
		// Ability-related properties for gadgets
		public Dictionary<uint, ConfigAbility> AbilityHashMap = new();
		public Dictionary<string, Dictionary<string, float>?>? AbilitySpecials = new();
		public HashSet<string> ActiveDynamicAbilities = new();
		public Dictionary<string, HashSet<string>> UnlockedTalentParams = new();

		public GadgetEntity(Session session, uint gadgetId, GadgetLua? gadgetInfo, Vector3? position, Vector3? rotation, uint? entityId = null)
		: base(session, position, rotation, ProtEntityType.ProtEntityGadget, entityId)
		{
			_gadgetId = gadgetId;
			_gadgetLua = gadgetInfo;
			level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel;
			gadgetExcel = MainApp.resourceManager.GadgetExcel[gadgetId];
			abilityManager = new GadgetAbilityManager(this);
			InitAbilityStuff();
			abilityManager.Initialize();
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

		public Retcode onClientExecuteRequest(int param1, int param2, int param3)
		{
			Dictionary<uint, string> gadgetLuas = MainApp.resourceManager.GadgetLuaConfig;
			if (!gadgetLuas.TryGetValue(_gadgetId, out string? luaFile) || string.IsNullOrEmpty(luaFile))
			{
				session.c.LogError($"GadgetLua for gadgetId {_gadgetId} not found");
				return Retcode.RetSvrError;
			}
			string luaPath = Path.Combine(MainApp.resourceManager.loader.LuaPath, "gadget", luaFile + ".lua");

			using (NLua.Lua tGadgetLua = new NLua.Lua())
			{
				ScriptLib scriptLib = new(session);
				scriptLib.currentSession = session;
				scriptLib.currentGroupId = (int)_gadgetLua!.group_id;
                tGadgetLua["ScriptLib"] = scriptLib;
                tGadgetLua["context_"] = session;
				//groupLua["evt_"] = args.toTable();

				string luaStr = LuaManager.GetCommonScriptConfigAsLua() + "\n"
						+ LuaManager.GetConfigEntityTypeEnumAsLua() + "\n"
						+ LuaManager.GetConfigEntityEnumAsLua() + "\n"
						+ File.ReadAllText(luaPath);

				try
				{
					tGadgetLua.DoString(luaStr.Replace("ScriptLib.", "ScriptLib:"));
				}
				catch (Exception ex)
				{
					session.c.LogError($"Error occured in loading Gadget Lua {ex.Message}");
					return Retcode.RetSvrError;
                }
            }
			return Retcode.RetSucc;
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
		
		/// <summary>
		/// Initialize ability system for this gadget entity
		/// </summary>
		public void InitAbilityStuff()
		{
			if (gadgetExcel == null || string.IsNullOrEmpty(gadgetExcel.jsonName))
			{
				return; // should not happen
			}
			if (
				!MainApp.resourceManager.ConfigGadgetMap.TryGetValue(gadgetExcel.jsonName, out ConfigGadget? gconfig) || gconfig == null)
			{ 
				// should not happen
				session.c.LogWarning($"{gadgetExcel.jsonName} does not exist in binoutput");
				return;
			}
			if (gconfig.abilities == null)
				// no abilities
				return;
			foreach (TargetAbility targetAbility in gconfig.abilities)
			{
				if (!MainApp.resourceManager.ConfigAbilityMap.TryGetValue(targetAbility.abilityName, out ConfigAbilityContainer? container))
				{
					session.c.LogError($"gadget ability {targetAbility.abilityName} not found in binoutput");
					continue;
				}

				if (container.Default is ConfigAbility ability)
				{
					uint abilityHash = (uint)Ability.Utils.AbilityHash(ability.abilityName);
					AbilityHashMap[abilityHash] = ability;
				}
			}



			File.WriteAllText($"Test/{gadgetExcel.jsonName}.json", JsonConvert.SerializeObject(AbilityHashMap, Formatting.Indented));

        }
	}
}
