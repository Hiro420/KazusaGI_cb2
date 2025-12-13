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
using System;
using System.IO;

namespace KazusaGI_cb2.GameServer
{
	public class GadgetEntity : Entity, IDamageable
	{
		public GadgetLua? _gadgetLua;
		public uint _gadgetId;
		public GadgetExcelConfig gadgetExcel;
		public ConfigGadget? configGadget;
		public uint level;
		public bool isLockHP => configGadget?.combat?.property?.isLockHP ?? false;
		public uint StateBeginTime { get; private set; }

		public float Hp { get; private set; } = 1f;
		public float MaxHp { get; private set; } = 1f;

		public GadgetState state => _gadgetLua?.state ?? GadgetState.Default;

		public HashSet<uint> WorktopOptions { get; } = new();

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

			if (
				!MainApp.resourceManager.ConfigGadgetMap.TryGetValue(gadgetExcel.jsonName, out configGadget) || configGadget == null)
			{
				// should not happen
				if (!string.IsNullOrEmpty(gadgetExcel.jsonName))
					session.c.LogWarning($"{gadgetExcel.jsonName} does not exist in binoutput");
			}
			else
			{
				var combatdata = configGadget.combat;
				if (combatdata != null && combatdata.property != null)
				{
					MaxHp = combatdata.property.HP;
					Hp = MaxHp;
				}
			}

			// Initialize state begin time similarly to Gadget::state_begin_time_
			StateBeginTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
				scriptLib.currentGroupId = (int)(_gadgetLua?.group_id ?? 0);
				scriptLib.currentGadget = this;
				tGadgetLua["ScriptLib"] = scriptLib;
				tGadgetLua["context_"] = session;

				string luaStr = LuaManager.GetCommonScriptConfigAsLua() + "\n"
						+ LuaManager.GetConfigEntityTypeEnumAsLua() + "\n"
						+ LuaManager.GetConfigEntityEnumAsLua() + "\n"
						+ File.ReadAllText(luaPath);

				try
				{
					tGadgetLua.DoString(luaStr.Replace("ScriptLib.", "ScriptLib:"));
					var func = tGadgetLua.GetFunction("OnClientExecuteReq");
					if (func == null)
					{
						session.c.LogError($"OnClientExecuteReq not found in gadget lua for gadgetId {_gadgetId}");
						return Retcode.RetSvrError;
					}

					// In hk4e this passes ScriptContext plus param1-3; our context
					// is the current session, which ScriptLib methods use together
					// with currentGadget/currentGroupId.
					func.Call(session, param1, param2, param3);
				}
				catch (Exception ex)
				{
					session.c.LogError($"Error occured executing Gadget Lua OnClientExecuteReq: {ex.Message}");
					return Retcode.RetSvrError;
				}
			}
			return Retcode.RetSucc;
		}

		protected override void BuildKindSpecific(SceneEntityInfo ret)
		{
			ret.Name = gadgetExcel.jsonName;

			// Determine born type similarly to hk4e's Gadget::toClient:
			// 1) Prefer the script-configured born_type if present.
			// 2) If none, derive from GadgetExcelConfig.type.
			var bornType = _gadgetLua?.born_type ?? GadgetBornType.GadgetBornNone;
			if (bornType == GadgetBornType.GadgetBornNone)
			{
				var type = gadgetExcel.type;
				// In hk4e, when born_type_ is 0:
				//   if (type <= 24) born_type = (type >= 23) ? 1 : 0; // IN_AIR for AirflowField/SpeedupField
				//   else if (type == 34) born_type = 6;               // GROUND for EnvAnimal
				if (type <= GadgetType_Excel.SpeedupField)
				{
					if (type >= GadgetType_Excel.AirflowField)
						bornType = GadgetBornType.GadgetBornInAir;
				}
				else if (type == GadgetType_Excel.EnvAnimal)
				{
					bornType = GadgetBornType.GadgetBornGround;
				}
			}

			var info = new SceneGadgetInfo
			{
				AuthorityPeerId = session.player!.PeerId,
				GadgetState = (uint)state,
				IsEnableInteract = state == GadgetState.Default,
				ConfigId = _gadgetLua?.config_id ?? 0,
				GroupId = _gadgetLua?.group_id ?? 0,
				GadgetId = _gadgetId,
				BornType = bornType,
				GadgetType = (uint)(_gadgetLua?.type ?? 0)
			};

			// If this is a gather gadget (GatherPoint/GatherObject) and the script
			// provides a non-zero point_type, mirror hk4e by looking up the
			// corresponding GatherExcelConfig and wiring its itemId into
			// GatherGadgetInfo for the client.
			if (_gadgetLua != null && _gadgetLua.point_type != 0 &&
				(gadgetExcel.type == GadgetType_Excel.GatherPoint || gadgetExcel.type == GadgetType_Excel.GatherObject))
			{
				if (MainApp.resourceManager.GatherExcel.TryGetValue(_gadgetLua.point_type, out var gatherCfg))
				{
					info.GatherGadget = new GatherGadgetInfo
					{
						ItemId = gatherCfg.itemId,
						IsForbidGuest = false
					};
				}
			}

			// Mirror hk4e's script-config wiring for gadget owner and cutscene flags.
			// "owner" in gadget lua refers to another gadget's config_id within the
			// same group; we resolve it to that gadget's runtime entity id if present.
			if (_gadgetLua != null && _gadgetLua.owner != 0 && session.player?.Scene != null)
			{
				var scene = session.player.Scene;
				var ownerEnt = scene.EntityManager.Entities.Values
					.OfType<GadgetEntity>()
					.FirstOrDefault(e => e._gadgetLua != null
						&& e._gadgetLua.group_id == _gadgetLua.group_id
						&& e._gadgetLua.config_id == _gadgetLua.owner);

				if (ownerEnt != null)
					info.OwnerEntityId = ownerEnt._EntityId;
			}

			// showcutscene in gadget lua directly maps to SceneGadgetInfo.IsShowCutscene.
			info.IsShowCutscene = _gadgetLua?.showcutscene ?? false;
			
			ret.Gadget = info;
		}

		public void ApplyDamage(float amount, AttackResult attack)
		{
			// Mirror hk4e gadget OnBeHurt callback: notify gadget lua
			// about elemental hits before applying HP changes.
			try
			{
				OnBeHurt(attack, true);
			}
			catch (Exception ex)
			{
				session.c.LogError($"Error occured executing Gadget Lua OnBeHurt: {ex.Message}");
			}

			if (isLockHP)
				return;

			// TODO: Handle by abilities
			Hp = MathF.Max(0f, Hp - amount);

			var upd = new EntityFightPropUpdateNotify { EntityId = _EntityId };
			upd.FightPropMaps[(uint)FightPropType.FIGHT_PROP_CUR_HP] = Hp;
			session.SendPacket(upd);

			if (Hp <= 0f)
			{
				// Mirror hk4e gadget OnDie callback: notify gadget lua
				// about the final blow that destroyed this gadget before
				// running generic death/cleanup logic.
				try
				{
					OnDie(attack);
				}
				catch (Exception ex)
				{
					session.c.LogError($"Error occured executing Gadget Lua OnDie: {ex.Message}");
				}

				this.OnDied(Protocol.VisionType.VisionDie);
			}
		}

		public void OnDie(AttackResult attack)
		{
			Dictionary<uint, string> gadgetLuas = MainApp.resourceManager.GadgetLuaConfig;
			if (!gadgetLuas.TryGetValue(_gadgetId, out string? luaFile) || string.IsNullOrEmpty(luaFile))
				return;

			string luaPath = Path.Combine(MainApp.resourceManager.loader.LuaPath, "gadget", luaFile + ".lua");

			using (NLua.Lua tGadgetLua = new NLua.Lua())
			{
				ScriptLib scriptLib = new(session);
				scriptLib.currentSession = session;
				scriptLib.currentGroupId = (int)(_gadgetLua?.group_id ?? 0);
				scriptLib.currentGadget = this;
				tGadgetLua["ScriptLib"] = scriptLib;
				tGadgetLua["context_"] = session;

				string luaStr = LuaManager.GetCommonScriptConfigAsLua() + "\n"
						+ LuaManager.GetConfigEntityTypeEnumAsLua() + "\n"
						+ LuaManager.GetConfigEntityEnumAsLua() + "\n"
						+ File.ReadAllText(luaPath);

				if (!luaStr.Contains("function OnDie"))
					return; // avoid loading lua if not needed

				tGadgetLua.DoString(luaStr.Replace("ScriptLib.", "ScriptLib:"));
				var func = tGadgetLua.GetFunction("OnDie");
				if (func == null)
					return; // optional in hk4e

				// Lua signature: OnDie(context, element_type, strike_type)
				func.Call(session, (int)attack.ElementType, (int)attack.StrikeType);
			}
		}

		public void OnBeHurt(AttackResult attack, bool isHost)
		{
			Dictionary<uint, string> gadgetLuas = MainApp.resourceManager.GadgetLuaConfig;
			if (!gadgetLuas.TryGetValue(_gadgetId, out string? luaFile) || string.IsNullOrEmpty(luaFile))
				return;

			string luaPath = Path.Combine(MainApp.resourceManager.loader.LuaPath, "gadget", luaFile + ".lua");

			using (NLua.Lua tGadgetLua = new NLua.Lua())
			{
				ScriptLib scriptLib = new(session);
				scriptLib.currentSession = session;
				scriptLib.currentGroupId = (int)(_gadgetLua?.group_id ?? 0);
				scriptLib.currentGadget = this;
				tGadgetLua["ScriptLib"] = scriptLib;
				tGadgetLua["context_"] = session;

				string luaStr = LuaManager.GetCommonScriptConfigAsLua() + "\n"
						+ LuaManager.GetConfigEntityTypeEnumAsLua() + "\n"
						+ LuaManager.GetConfigEntityEnumAsLua() + "\n"
						+ File.ReadAllText(luaPath);

				if (!luaStr.Contains("function OnBeHurt"))
					return; // avoid loading lua if not needed

				tGadgetLua.DoString(luaStr.Replace("ScriptLib.", "ScriptLib:"));
				var func = tGadgetLua.GetFunction("OnBeHurt");
				if (func == null)
					return; // optional in hk4e

				// Lua signature: OnBeHurt(context, element_type, strike_type, is_host)
				func.Call(session, (int)attack.ElementType, (int)attack.StrikeType, isHost);
			}
		}

		public void OnTimer(uint now)
		{
			Dictionary<uint, string> gadgetLuas = MainApp.resourceManager.GadgetLuaConfig;
			if (!gadgetLuas.TryGetValue(_gadgetId, out string? luaFile) || string.IsNullOrEmpty(luaFile))
				return;

			string luaPath = Path.Combine(MainApp.resourceManager.loader.LuaPath, "gadget", luaFile + ".lua");

			using (NLua.Lua tGadgetLua = new NLua.Lua())
			{
				ScriptLib scriptLib = new(session);
				scriptLib.currentSession = session;
				scriptLib.currentGroupId = (int)(_gadgetLua?.group_id ?? 0);
				scriptLib.currentGadget = this;
				tGadgetLua["ScriptLib"] = scriptLib;
				tGadgetLua["context_"] = session;

				string luaStr = LuaManager.GetCommonScriptConfigAsLua() + "\n"
						+ LuaManager.GetConfigEntityTypeEnumAsLua() + "\n"
						+ LuaManager.GetConfigEntityEnumAsLua() + "\n"
						+ File.ReadAllText(luaPath);

				if (!luaStr.Contains("function OnTimer"))
					return; // avoid loading lua if not needed

				tGadgetLua.DoString(luaStr.Replace("ScriptLib.", "ScriptLib:"));
				var func = tGadgetLua.GetFunction("OnTimer");
				if (func == null)
					return; // optional in hk4e

				// Lua signature: OnTimer(context, now)
				func.Call(session, (int)now);
			}
		}

		protected override void OnDied(Protocol.VisionType disappearType)
		{
			base.OnDied(disappearType);

			// Fire EVENT_ANY_GADGET_DIE triggers for this gadget's group/config,
			// mirroring MonsterEntity's EVENT_ANY_MONSTER_DIE handling.
			if (_gadgetLua == null || session.player == null)
				return;

			var group = session.player.Scene.GetGroup((int)_gadgetLua.group_id);
			if (group == null)
				return;

			LuaManager.executeTriggersLua(
				session,
				group,
				new Lua.ScriptArgs(
					(int)_gadgetLua.group_id,
					(int)Lua.TriggerEventType.EVENT_ANY_GADGET_DIE,
					(int)_gadgetLua.config_id));
		}

		public void ChangeState(GadgetState newState)
		{
			if (_gadgetLua == null) return;

			var old = _gadgetLua.state;
			_gadgetLua.state = newState;
			StateBeginTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

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
			if (configGadget?.abilities == null)
				// no abilities
				return;
			foreach (TargetAbility targetAbility in configGadget.abilities)
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


			// Directory.CreateDirectory("Test");
			// File.WriteAllText($"Test/{gadgetExcel.jsonName}.json", JsonConvert.SerializeObject(AbilityHashMap, Formatting.Indented));

		}
	}
}
