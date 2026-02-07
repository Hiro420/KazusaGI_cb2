using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.ServerExcel;
using NLua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static KazusaGI_cb2.Protocol.ScenePlayerSoundNotify;

namespace KazusaGI_cb2.GameServer.Lua;

// todo: group vars, GetGroupVariableValueByGroup

public class ScriptLib
{
    public int currentGroupId;
    public Session currentSession;
    public ScriptArgs? currentEventArgs;
    public GadgetEntity? currentGadget;

    private SceneGroupLua? GetCurrentGroup()
    {
        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return null;
        if (currentGroupId == 0)
            return null;
        return player.Scene.GetGroup(currentGroupId);
    }

    public int GetGroupMonsterCount(Session session)
    {
        Log("Called GetGroupMonsterCount");
        return currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is MonsterEntity monster &&
                monster.Hp > 0 &&
				monster._monsterInfo != null &&
                monster._monsterInfo.group_id == currentGroupId)
            .Count();
    }

    public int GetContextGadgetConfigId(Session session)
    {
        Log("Called GetContextGadgetConfigId");
        if (currentGadget == null || currentGadget._gadgetLua == null)
        {
            Log("GetContextGadgetConfigId called with no current gadget");
            return -1;
        }
        return (int)currentGadget._gadgetLua.config_id;
	}

    public int GetContextGroupId(Session session)
    {
        Log("Called GetContextGroupId");
        return currentGroupId;
	}

	public int GetGatherConfigIdList(Session session)
    {
        Log("Called GetGatherConfigIdList");

        foreach (GadgetEntity gadgetEntity in currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity g && g.gadgetExcel != null)
            .Cast<GadgetEntity>())
        {
            GatherExcelConfig? gatherConfig = MainApp.resourceManager.GatherExcel.FirstOrDefault(i =>
                i.Value.gadgetId == gadgetEntity._gadgetId
            ).Value;
            if (gatherConfig != null)
            {
                return (int)gatherConfig.id;
            }
		}

		return 0;
    }

    public int MarkPlayerAction(Session session,uint param1, uint param2, uint param3)
    {
        Log($"Called MarkPlayerAction action_type={param1} action_param={param2} action_count={param3}");
        return 0;
    }

	public int TowerMirrorTeamSetUp(Session session, int tower_team_id)
    {
        Log($"Called TowerMirrorTeamSetUp tower_team_id={tower_team_id}");

        var player = currentSession.player;
        if (player == null)
            return -1;

        if (player.towerInstance == null)
        {
            currentSession.c.LogWarning("[ScriptLib] TowerMirrorTeamSetUp called but towerInstance is null");
            return -1;
        }

        if (tower_team_id <= 0)
        {
            tower_team_id = 1;
        }

        return player.towerInstance.MirrorTeamSetUp((uint)tower_team_id);
    }

    public int TowerCountTimeStatus(Session session, int status)
    {
        // Tower-specific timer handling. For now we simply accept the
        // call and log it so tower scripts can run without error.
        Log($"Called TowerCountTimeStatus status={status}");
        return 0;
    }

	// ScriptLib::LockForce(const ScriptContext *context, uint32_t force_id)
	public int LockForce(Session session, uint force_id)
    {
        Log($"Called LockForce force_id={force_id}");
        return 0;
	}

    // ScriptLib::UnlockForce(const ScriptContext *context, uint32_t force_id)
    public int UnlockForce(Session session, uint force_id)
    {
        Log($"Called UnlockForce force_id={force_id}");
        return 0;
	}

	public int DropSubfield(Session session, object _table)
    {
	    LuaTable? table = _table as LuaTable;
	    if (table == null)
		    return 0;
	    string subfield_name = (string)table["subfield_name"];

        Log($"Called DropSubfield subfield_name={subfield_name}");

        ResourceManager rm = MainApp.resourceManager;

        if (rm == null)
            return 1;

        // we use reflection to find the subfield by name
        EntityDropSubfieldRow? subfieldRow = null;
        int foundIndex = -1;

		foreach (EntityDropSubfieldRow row in rm.ServerEntityDropSubfieldRows)
        {
			for (int i = 1; i < 8; i++)
			{
				string propName = $"Branch{i}Type";
				var prop = typeof(EntityDropSubfieldRow).GetProperty(propName);
				if (prop == null)
					continue;
				if (prop.GetValue(row) as string == subfield_name)
				{
					subfieldRow = row;
					foundIndex = i;
					break;
				}
			}
		}

		if (foundIndex == -1 || subfieldRow == null)
        {
            Log($"DropSubfield: subfield_name {subfield_name} not found for entity {currentEventArgs!.param1}");
            return 1;
		}

		// Get the pool ID for the found subfield
        var poolIdProp = typeof(EntityDropSubfieldRow).GetProperty($"Branch{foundIndex}PoolId");
        if (poolIdProp == null)
        {
            Log($"DropSubfield: could not find pool ID property for subfield {subfield_name}");
            return 1;
        }
        int value = poolIdProp.GetValue(subfieldRow) as int? ?? 0;
        Log($"DropSubfield: found pool ID {value} for subfield {subfield_name}");

        DropSubfieldRow? dropSubfieldRow = rm.ServerDropSubfieldRows
            .FirstOrDefault(ds => ds.SubfieldPoolId == value);

        if (dropSubfieldRow == null)
        {
            Log($"DropSubfield: no DropSubfieldRow found for pool ID {value}");
            return 1;
		}

        DropTreeRow? dropTreeRow = rm.ServerDropTreeRows
            .FirstOrDefault(dt => dt.Id == dropSubfieldRow.DropId);

        if (dropTreeRow == null)
        {
            Log($"DropSubfield: no DropTreeRow found for drop ID {dropSubfieldRow.DropId}");
            return 1;
        }

        if (currentGadget == null)
        {
            Log("DropSubfield: currentGadget is null, dropping loot failed");
            return 1;
		}

		DropManager.DropLoot(
			currentSession,
			(uint)dropSubfieldRow.DropId,
			currentGadget
		);

		return 0;
    }

    public int GetRegionEntityCount(Session session, object _table)
    {
        Log("Called GetRegionEntityCount");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return 0;

        LuaTable? table = _table as LuaTable;
        if (table == null)
            return 0;

        int regionConfigId;
        int entityTypeRaw;

        try
        {
            regionConfigId = (int)(long)table["region_eid"];
            entityTypeRaw = (int)(long)table["entity_type"];
        }
        catch
        {
            // Malformed call from Lua; mirror hk4e by returning 0.
            return 0;
        }

        var entityType = (EntityType)entityTypeRaw;
        return player.Scene.GetRegionEntityCount(regionConfigId, entityType);
    }

    // Mirror hk4e's ScriptLib gadget APIs using the bound currentGadget set by GadgetEntity before executing lua.

    public int GetGadgetState(Session session)
    {
        if (currentGadget == null || currentGadget._gadgetLua == null)
        {
            Log("GetGadgetState called with no current gadget");
            return (int)GadgetState.Default;
        }

        return (int)currentGadget._gadgetLua.state;
    }

    public int PrintLog(Session session, string msg)
    {
        Log($"PrintLog: {msg}");
        return 0;
	}


	public int SetGadgetState(Session session, int state)
    {
        if (currentGadget == null)
        {
            Log("SetGadgetState called with no current gadget");
            return -1;
        }

        currentGadget.ChangeState((GadgetState)state);
        return 0;
    }

    public uint GetGadgetStateBeginTime(Session session)
    {
        if (currentGadget == null)
        {
            Log("GetGadgetStateBeginTime called with no current gadget");
            return 0;
        }

        return currentGadget.StateBeginTime;
    }

    public int AddExtraGroupSuite(Session session, int group_id, int suite_index)
    {
        Log("Called AddExtraGroupSuite");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] AddExtraGroupSuite called with no active player/scene");
            return -1;
        }

        uint gid = (uint)group_id;
        uint sid = (uint)suite_index;
        return player.Scene.AddExtraGroupSuite(gid, sid);
    }

    public int RemoveExtraGroupSuite(Session session, int group_id, int suite_index)
    {
        Log("Called RemoveExtraGroupSuite");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] RemoveExtraGroupSuite called with no active player/scene");
            return -1;
        }
        uint gid = (uint)group_id;
        uint sid = (uint)suite_index;
        return player.Scene.RemoveExtraGroupSuite(gid, sid);
	}

    public int KillGroupEntity(Session session, object _param)
    {
        Log("Called KillGroupEntity");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] KillGroupEntity called with no active player/scene");
            return -1;
        }

        LuaTable? param = _param as LuaTable;
        if (param == null)
        {
            currentSession.c.LogWarning("[ScriptLib] KillGroupEntity called with invalid param");
            return -1;
        }

        var scene = player.Scene;

        int groupId = currentGroupId;
        try
        {
            if (param["group_id"] != null)
            {
                groupId = (int)(long)param["group_id"];
            }
        }
        catch
        {
            // shouldn't be a thing
        }

        var group = scene.GetGroup(groupId);
        if (group == null)
        {
            currentSession.c.LogWarning($"[ScriptLib] KillGroupEntity: group_ptr is null, group_id: {groupId}");
            return -1;
        }

		GroupKillPolicy killPolicy = GroupKillPolicy.GROUP_KILL_NONE;
        try
        {
            if (param["kill_policy"] != null)
            {
                killPolicy = (GroupKillPolicy)(int)(long)param["kill_policy"];
            }
        }
        catch
        {
            // wtf?
        }

        if (killPolicy == GroupKillPolicy.GROUP_KILL_ALL)
        {
            var entitiesToKill = scene.EntityManager.Entities.Values
                .Where(e =>
                    ((e is MonsterEntity monster && monster._monsterInfo != null && monster._monsterInfo.group_id == groupId) ||
                     (e is GadgetEntity gadget && gadget._gadgetLua != null && gadget._gadgetLua.group_id == groupId) ||
                     (e is NpcEntity npc && npc._npcInfo != null && npc._npcInfo.group_id == groupId)))
                .ToList();
            foreach (var entity in entitiesToKill)
            {
                entity.ForceKill();
            }
            return 0;
        }
        else if (killPolicy == GroupKillPolicy.GROUP_KILL_MONSTER)
        {
            var monsters = scene.EntityManager.Entities.Values
                .Where(e => e is MonsterEntity monster &&
                            monster._monsterInfo != null &&
                            monster._monsterInfo.group_id == groupId)
                .ToList();
            foreach (var entity in monsters)
            {
                entity.ForceKill();
            }
            return 0;
        }
        else if (killPolicy == GroupKillPolicy.GROUP_KILL_GADGET)
        {
            var gadgets = scene.EntityManager.Entities.Values
                .Where(e => e is GadgetEntity gadget &&
                            gadget._gadgetLua != null &&
                            gadget._gadgetLua.group_id == groupId)
                .ToList();
            foreach (var entity in gadgets)
            {
                entity.ForceKill();
            }
            return 0;
        }
        else if (killPolicy == GroupKillPolicy.GROUP_KILL_NPC)
        {
            var npcs = scene.EntityManager.Entities.Values
                .Where(e => e is NpcEntity npc &&
                            npc._npcInfo != null &&
                            npc._npcInfo.group_id == groupId)
                .ToList();
            foreach (var entity in npcs)
            {
                entity.ForceKill();
            }
            return 0;
        }

        try
        {
            if (param["monsters"] is LuaTable monstersTable)
            {
                var monsterConfigIds = new HashSet<uint>();
                foreach (var key in monstersTable.Keys)
                {
                    try
                    {
                        uint configId = (uint)(long)monstersTable[key];
                        monsterConfigIds.Add(configId);
                    }
                    catch { }
                }

                if (monsterConfigIds.Count > 0)
                {
                    var monsters = scene.EntityManager.Entities.Values
                        .Where(e => e is MonsterEntity monster &&
                                    monster._monsterInfo != null &&
                                    monster._monsterInfo.group_id == groupId &&
                                    monsterConfigIds.Contains(monster._monsterInfo.config_id))
                        .ToList();
                    foreach (var entity in monsters)
                    {
                        entity.ForceKill();
                    }
                }
            }
        }
        catch { }

        try
        {
            if (param["gadgets"] is LuaTable gadgetsTable)
            {
                var gadgetConfigIds = new HashSet<uint>();
                foreach (var key in gadgetsTable.Keys)
                {
                    try
                    {
                        uint configId = (uint)(long)gadgetsTable[key];
                        gadgetConfigIds.Add(configId);
                    }
                    catch { }
                }

                if (gadgetConfigIds.Count > 0)
                {
                    var gadgets = scene.EntityManager.Entities.Values
                        .Where(e => e is GadgetEntity gadget &&
                                    gadget._gadgetLua != null &&
                                    gadget._gadgetLua.group_id == groupId &&
                                    gadgetConfigIds.Contains((uint)gadget._gadgetLua.config_id))
                        .ToList();
                    foreach (var entity in gadgets)
                    {
                        entity.ForceKill();
                    }
                }
            }
        }
        catch { }

        try
        {
            if (param["npcs"] is LuaTable npcsTable)
            {
                var npcConfigIds = new HashSet<uint>();
                foreach (var key in npcsTable.Keys)
                {
                    try
                    {
                        uint configId = (uint)(long)npcsTable[key];
                        npcConfigIds.Add(configId);
                    }
                    catch { }
                }

                if (npcConfigIds.Count > 0)
                {
                    var npcs = scene.EntityManager.Entities.Values
                        .Where(e => e is NpcEntity npc &&
                                    npc._npcInfo != null &&
                                    npc._npcInfo.group_id == groupId &&
                                    npcConfigIds.Contains(npc._npcInfo.config_id))
                        .ToList();
                    foreach (var entity in npcs)
                    {
                        entity.ForceKill();
                    }
                }
            }
        }
        catch { }

        return 0;
    }

    public int GetCurTriggerCount(Session session)
    {
        Log("Called GetCurTriggerCount");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] GetCurTriggerCount called with no active player/scene");
            return 0;
        }

        // In hk4e, this reads ScriptContext::trigger_ptr->trigger_count.
        // We mirror that by having Scene track a per-trigger execution
        // counter and exposing the active trigger's count via
        // Scene.currentTriggerCount, which is updated by LuaManager
        // right before executing the trigger's Lua.

        Log($"GetCurTriggerCount returning {player.Scene.currentTriggerCount}");

		return player.Scene.currentTriggerCount;
    }

    public int CreateGroupTimerEvent(Session session, uint group_id, string timer_name, float delay_time)
    {
        Log("Called CreateGroupTimerEvent");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] CreateGroupTimerEvent called with no active player/scene");
            return -1;
        }
        if (group_id == 0)
        {
            currentSession.c.LogWarning("[ScriptLib] CreateGroupTimerEvent called with group_id = 0");
            return -1;
        }
        var scene = player.Scene;
        var group = scene.GetGroup((int)group_id);
        if (group == null)
        {
            currentSession.c.LogWarning("[ScriptLib] CreateGroupTimerEvent called but group not found in scene");
            return -1;
        }
        float delay_time_ms = delay_time * 1000.0f;
	    return scene.CreateGroupTimerEvent(group_id, timer_name, delay_time_ms);
    }

    public int CancelGroupTimerEvent(Session session, uint group_id, string name)
    {
        Log("Called CancelGroupTimerEvent");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] CancelGroupTimerEvent called with no active player/scene");
            return -1;
        }
        if (group_id == 0)
        {
            currentSession.c.LogWarning("[ScriptLib] CancelGroupTimerEvent called with group_id = 0");
            return -1;
        }
        var scene = player.Scene;
        var group = scene.GetGroup((int)group_id);
        if (group == null)
        {
            currentSession.c.LogWarning("[ScriptLib] CancelGroupTimerEvent called but group not found in scene");
            return -1;
        }
        return scene.CancelGroupTimerEvent(group_id, name);
	}

    public int GoToGroupSuite(Session session, int group_id, int suite_index)
    {
        Log("Called GoToGroupSuite");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] GoToGroupSuite called with no active player/scene");
            return -1;
        }
        var scene = player.Scene;
        var group = scene.GetGroup(group_id);
        if (group == null)
        {
            currentSession.c.LogWarning("[ScriptLib] GoToGroupSuite called but group not found in scene");
            return -1;
        }
        return scene.GoToGroupSuite((uint)group_id, (uint)suite_index);
	}

	public int SetIsAllowUseSkill(Session session, int is_allow_use_skill)
    {
        Log("Called SetIsAllowUseSkill");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
        {
            currentSession.c.LogWarning("[ScriptLib] SetIsAllowUseSkill called with no active player/scene");
            return -1;
        }

        bool allow = is_allow_use_skill != 0;

        // hk4e applies this to all players in the scene via Scene::foreachPlayer.
        // Our Scene currently owns a single Player instance, so we mirror the
        // behavior by updating the current scene's player.
        player.SetIsAllowUseSkill(allow);

        return 0;
    }

    public int GetGroupMonsterCountByGroupId(Session session, int groupId)
    {
        Log("Called GetGroupMonsterCountByGroupId");
        return currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is MonsterEntity monster &&
                        monster._monsterInfo != null &&
                        monster._monsterInfo.group_id == groupId)
            .Count();
    }

    public int ScenePlaySound(Session session, string sound_name, int play_type, bool is_broadcast)
    {
        Log("Called ScenePlaySound");
        ScenePlayerSoundNotify scenePlaySoundNotify = new ScenePlayerSoundNotify()
        {
            SoundName = sound_name,
            PlayType = (PlaySoundType)play_type
        };
        if (is_broadcast)
        {
            //currentSession.player!.Scene.BroadcastPacket(scenePlaySoundNotify); // todo
        }
        else
        {
            currentSession.SendPacket(scenePlaySoundNotify);
        }
        return 0;
    }

    public int GetGroupVariableValue(Session session, string var_name)
    {
        Log("Called GetGroupVariableValue");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return 0;

        if (currentGroupId == 0)
            return 0;

        return GetGroupVariableValueByGroup(session, var_name, currentGroupId);
    }

    // ScriptLib.ShowReminder(context, 400004)
    public int ShowReminder(Session session, int reminder_id)
    {
        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;
        var notify = new DungeonShowReminderNotify()
        {
            ReminderId = (uint)reminder_id
        };
        currentSession.SendPacket(notify);
        return 0;
    }

    // ScriptLib.AddQuestProgress(context, "133103106_progress1")
    public int AddQuestProgress(Session session, string quest_param)
    {
        var player = currentSession.player;
        if (player == null)
            return -1;
        // todo: quest manager
        return 0;
    }

    // ScriptLib.PlayCutScene(context, 310624801, 0)
    public int PlayCutScene(Session session, int cutscene_id, int wait_time)
    {
        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;
        if (wait_time > 0)
        {
            currentSession.c.LogWarning("[ScriptLib] PlayCutScene wait_time > 0 is not supported yet, executing immediately");
	    }
	    var notify = new CutSceneBeginNotify()
        {
            CutsceneId = (uint)cutscene_id
        };
        currentSession.SendPacket(notify);
        return 0;
    }

    // ScriptLib.ScenePlaySound(context, {play_pos = pos, sound_name = "LevelHornSound001", play_type= 1, is_broadcast = false })
    public int ScenePlaySound(Session session, object _table)
    {
        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;
        LuaTable? table = _table as LuaTable;
        string sound_name = (string)table!["sound_name"];
        int play_type = (int)(long)table["play_type"];
        bool is_broadcast = (bool)table["is_broadcast"];
        //if (is_broadcast)
        //{
        //    //currentSession.player!.Scene.BroadcastPacket(scenePlaySoundNotify); // todo
        //}
        //else
        {
            currentSession.SendPacket(new ScenePlayerSoundNotify()
            {
                SoundName = sound_name,
                PlayType = (PlaySoundType)play_type
            });
        }
        return 0;
    }


    public int GetGroupVariableValueByGroup(Session session, string var_name, int group_id)
    {
        Log("Called GetGroupVariableValueByGroup");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return 0;

        // Find the target group in the current scene.
        var scene = player.Scene;
        var group = scene.GetGroup(group_id);
        if (group == null)
            return 0;

        if (!group.variables.TryGetValue(var_name, out var value))
            return 0;

        // Lua group variables are integer-valued.
        try
        {
            return (int)value;
        }
        catch
        {
            return 0;
        }
    }

    public int SetGroupVariableValue(Session session, string var_name, int value)
    {
        Log("Called SetGroupVariableValue");

        var group = GetCurrentGroup();
        if (group == null)
            return -1;

        int oldValue = 0;
        group.variables.TryGetValue(var_name, out oldValue);
        if (oldValue == value)
            return 0;

        group.variables[var_name] = value;

        // Fire VARIABLE_CHANGE event like hk4e when a variable changes.
        // In hk4e, evt.param1 is the *new* value and evt.param2 is the
        // *old* value; Lua scripts (including the riddle gadgets) rely
        // on evt.param1 to reflect the updated variable state.
        if (currentEventArgs != null)
        {
            var args = new ScriptArgs(currentGroupId, (int)EventType.EVENT_VARIABLE_CHANGE)
            {
                param1 = value,    // new value
                param2 = oldValue, // old value
                source = var_name
            };

            LuaManager.executeTriggersLua(currentSession, group, args);
        }

        return 0;
    }

    public int ChangeGroupVariableValue(Session session, string var_name, int delta)
    {
        Log("Called ChangeGroupVariableValue");

        var group = GetCurrentGroup();
        if (group == null)
            return -1;

        group.variables.TryGetValue(var_name, out var oldValue);
        int newValue = oldValue + delta;

        group.variables[var_name] = newValue;

        // Mirror hk4e's EVENT_VARIABLE_CHANGE semantics: param1 is the
        // new value, param2 is the old value.
        if (currentEventArgs != null)
        {
            var args = new ScriptArgs(currentGroupId, (int)EventType.EVENT_VARIABLE_CHANGE)
            {
                param1 = newValue, // new value
                param2 = oldValue, // old value
                source = var_name
            };

            LuaManager.executeTriggersLua(currentSession, group, args);
        }

        return 0;
    }

    public int SetGroupVariableValueByGroup(Session session, string var_name, int value, int group_id)
    {
        Log("Called SetGroupVariableValueByGroup");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;

        var scene = player.Scene;
        var group = scene.GetGroup(group_id);
        if (group == null)
            return -1;

        group.variables.TryGetValue(var_name, out var oldValue);
        if (oldValue == value)
            return 0;

        group.variables[var_name] = value;

        // Fire VARIABLE_CHANGE for the target group so that dependent
        // triggers behave like hk4e. evt.param1 is the new value and
        // evt.param2 is the old value.
        var args = new ScriptArgs(group_id, (int)EventType.EVENT_VARIABLE_CHANGE)
        {
            param1 = value,    // new value
            param2 = oldValue, // old value
            source = var_name
        };

        LuaManager.executeTriggersLua(currentSession, group, args);

        return 0;
    }

    public int ChangeGroupGadget(Session session, object table_)
    {
        Log("Called ChangeGroupGadget");
        LuaTable? table = table_ as LuaTable;
        var configId = (int)(long)table!["config_id"];
        var state = (int)(long)table["state"];

        return SetGadgetStateByConfigId(session, configId, state);
    }

    public int SetGadgetStateByConfigId(Session session, int config_id, int state)
    {
        Log("Called SetGadgetStateByConfigId");
        IEnumerable<Entity> entities = currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity gadget &&
                        gadget._gadgetLua != null &&
                        gadget._gadgetLua.config_id == config_id);
        foreach (GadgetEntity gadget in entities)
            gadget.ChangeState((GadgetState)state);
        return 0;
    }

    public int SetGadgetEnableInteract(Session session, int group_id, int gadget_config_id, bool enable)
    {
        Log("Called SetGadgetEnableInteract");
        IEnumerable<Entity> entities = currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity gadget &&
                        gadget._gadgetLua != null &&
                        gadget._gadgetLua.config_id == gadget_config_id);
        foreach (GadgetEntity gadget in entities)
            gadget.SetEnableInteract(enable);
        return 0;
	}

	public int SetGroupGadgetStateByConfigId(Session session, int group_id, int config_id, int _state)
    {
        Log("Called SetGroupGadgetStateByConfigId");
        GadgetState state = (GadgetState)_state;
        IEnumerable<Entity> entities = currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity gadget &&
                        gadget._gadgetLua != null &&
                        gadget._gadgetLua.group_id == group_id &&
                        gadget._gadgetLua.config_id == config_id);
        foreach (GadgetEntity gadget in entities)
            gadget.ChangeState(state);
        return 0;
    }

    public int GetGadgetStateByConfigId(Session session, int group_id, int config_id)
    {
        Log("Called GetGadgetStateByConfigId");
        GadgetEntity? gadget = currentSession.player.Scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity g &&
                        g._gadgetLua != null &&
                        g._gadgetLua.group_id == group_id &&
                        g._gadgetLua.config_id == config_id)
            .FirstOrDefault() as GadgetEntity;
        if (gadget == null || gadget._gadgetLua == null)
            return -1;
        return (int)gadget._gadgetLua.state;
    }

    public int RefreshGroup(Session session, object _groupInfo)
    {
        Log("Called RefreshGroup");
        LuaTable? groupInfo = _groupInfo as LuaTable;
        if (groupInfo == null)
            return -1;

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;

        int groupId;
        try
        {
            groupId = (int)(long)groupInfo["group_id"];
        }
        catch
        {
            currentSession.c.LogWarning("[ScriptLib] RefreshGroup called without valid group_id");
            return -1;
        }

        int suite = 0;
        try
        {
            var suiteObj = groupInfo["suite"];
            if (suiteObj != null)
                suite = (int)(long)suiteObj;
        }
        catch
        {
            // suite is optional; when absent we pass 0 to mean
            // "use default/random suite" like hk4e.
        }

        // Optional refresh_level_revise parameter used by hk4e to scale
        // group level. We currently parse it for compatibility but do not
        // yet apply it to monster levels.
        try
        {
            var lvlObj = groupInfo["refresh_level_revise"];
            _ = lvlObj; // reserved for future use
        }
        catch
        {
        }

        SceneGroupLua? group = player.Scene.GetGroup(groupId);
        if (group == null)
            return -1;

        return player.Scene.RefreshGroup(group, suite);
    }

    public int ActiveChallenge(Session session, int source_name, int challenge_id, int param1, int param2, int param3, int param4)
    {
        Log("Called ActiveChallenge");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;

        if (currentGroupId == 0)
        {
            currentSession.c.LogWarning("[ScriptLib] ActiveChallenge called with currentGroupId = 0");
            return -1;
        }

        var scene = player.Scene;
        uint groupId = (uint)currentGroupId;
        var paramList = new uint[4]
        {
            (uint)param1,
            (uint)param2,
            (uint)param3,
            (uint)param4
        };

        return scene.BeginChallenge(groupId, (uint)source_name, (uint)challenge_id, paramList);
    }

    public int CreateMonster(Session session, object _config)
    {
        Log("Called CreateMonster");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;

        LuaTable? config = _config as LuaTable;
        if (config == null)
            return -1;

        float delayTime = 0;
        try
        {
            var delayObj = config["delay_time"];
            if (delayObj != null)
            {
                delayTime = (float)delayObj;
            }
        }
        catch
        {
            // ignore missing/invalid delay_time; spawn immediately
        }

        if (delayTime > 0)
        {
            currentSession.c.LogWarning($"[ScriptLib] CreateMonster delay_time={delayTime} currently ignored (spawning immediately)");
            // wait {delayTime} seconds before spawning
            Task.Delay(TimeSpan.FromSeconds(delayTime)).ContinueWith(_ =>
            {
                DoSpawnMonster(currentSession, config);
            });
        }
        else
        {
            DoSpawnMonster(currentSession, config);
        }

        return 0;
    }

    private void DoSpawnMonster(Session session, LuaTable config)
    {
        var player = currentSession.player;


        int configId;
        try
        {
            configId = (int)(long)config["config_id"];
        }
        catch
        {
            return;
        }

        int groupId = currentGroupId;
        try
        {
            if (config["group_id"] != null)
            {
                groupId = (int)(long)config["group_id"];
            }
        }
        catch
        {
            // ignore, fall back to currentGroupId
        }

        var scene = player.Scene;
        SceneGroupLua? group = scene.GetGroup(groupId);
        if (group == null || group.monsters == null)
            return;

        var monsterInfo = group.monsters.FirstOrDefault(m => m.config_id == (uint)configId);
        if (monsterInfo == null)
            return;

        var ent = new MonsterEntity(currentSession, monsterInfo.monster_id, monsterInfo, monsterInfo.pos, monsterInfo.rot);
        scene.EntityManager.Add(ent);

        if (!scene.alreadySpawnedMonsters.Contains(monsterInfo))
            scene.alreadySpawnedMonsters.Add(monsterInfo);

        var appear = new SceneEntityAppearNotify
        {
            AppearType = Protocol.VisionType.VisionMeet
        };
        appear.EntityLists.Add(ent.ToSceneEntityInfo());
        currentSession.SendPacket(appear);

        var args = new ScriptArgs(groupId, (int)EventType.EVENT_ANY_MONSTER_LIVE, (int)monsterInfo.config_id);
        LuaManager.executeTriggersLua(currentSession, group, args);

        return;
    }

    public int StopChallenge(Session session, int source_name, int is_success)
    {
        Log("Called StopChallenge");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;

        if (currentGroupId == 0)
        {
            currentSession.c.LogWarning("[ScriptLib] StopChallenge called with currentGroupId = 0");
            return -1;
        }

        bool success = is_success != 0;
        return player.Scene.StopChallenge((uint)currentGroupId, (uint)source_name, success);
    }

    public int CauseDungeonFail(Session session)
    {
        Log("Called CauseDungeonFail");

        var player = currentSession.player;
        if (player == null)
            return -1;

        // Send a basic dungeon settle notify marking failure.
        // More detailed settle_show / fail_cond can be added later.
        var notify = new DungeonSettleNotify
        {
            DungeonId = player.SceneId,
            IsSuccess = false,
            CloseTime = 10
        };

        currentSession.SendPacket(notify);

        return 0;
    }

    public int CreateGadget(Session session, object _config)
    {
        Log("Called CreateGadget");

        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;

        LuaTable? config = _config as LuaTable;
        if (config == null)
            return -1;

        int configId;
        try
        {
            configId = (int)(long)config["config_id"];
        }
        catch
        {
            return -1;
        }

        int groupId = currentGroupId;
        try
        {
            if (config["group_id"] != null)
            {
                groupId = (int)(long)config["group_id"];
            }
        }
        catch
        {
            // ignore, fall back to currentGroupId
        }

        var scene = player.Scene;
        SceneGroupLua? group = scene.GetGroup(groupId);
        if (group == null || group.gadgets == null)
            return -1;

        var gadgetInfo = group.gadgets.FirstOrDefault(g => g.config_id == configId);
        if (gadgetInfo == null)
            return -1;

	    var ent = new GadgetEntity(currentSession, gadgetInfo.gadget_id, gadgetInfo, gadgetInfo.pos, gadgetInfo.rot);
	    scene.EntityManager.Add(ent);

        if (!scene.alreadySpawnedGadgets.Contains(gadgetInfo))
            scene.alreadySpawnedGadgets.Add(gadgetInfo);

        var appear = new SceneEntityAppearNotify
        {
            AppearType = Protocol.VisionType.VisionMeet
        };
        appear.EntityLists.Add(ent.ToSceneEntityInfo());
        currentSession.SendPacket(appear);

        var args = new ScriptArgs(groupId, (int)EventType.EVENT_GADGET_CREATE, (int)gadgetInfo.config_id);
        LuaManager.executeTriggersLua(currentSession, group, args);

        return 0;
    }

    public int KillEntityByConfigId(Session session, object _config)
    {
        Log("Called KillEntityByConfigId");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;
        LuaTable? config = _config as LuaTable;
        if (config == null)
            return -1;
        int configId;
        try
        {
            configId = (int)(long)config["config_id"];
        }
        catch
        {
            return -1;
        }
        int groupId = currentGroupId;
        try
        {
            if (config["group_id"] != null)
            {
                groupId = (int)(long)config["group_id"];
            }
        }
        catch
        {
            // ignore, fall back to currentGroupId
        }
        var scene = player.Scene;
        var entitiesToRemove = scene.EntityManager.Entities.Values
            .Where(e =>
                (e is MonsterEntity monster &&
                 monster._monsterInfo != null &&
                 monster._monsterInfo.group_id == groupId &&
                 monster._monsterInfo.config_id == (uint)configId)
                ||
                (e is GadgetEntity gadget &&
                 gadget._gadgetLua != null &&
                 gadget._gadgetLua.group_id == groupId &&
                 gadget._gadgetLua.config_id == configId))
            .ToList();
        foreach (var entity in entitiesToRemove)
        {
            entity.ForceKill();
		}
        return 0;
	}

    public int SetMonsterBattleByGroup(Session session, int is_alert, int config_id, int group_id)
    {
        Log("Called SetMonsterBattleByGroup");
        var player = currentSession.player;
        if (player == null || player.Scene == null)
            return -1;
        var scene = player.Scene;
        var monsters = scene.EntityManager.Entities.Values
            .Where(e => e is MonsterEntity monster &&
                        monster._monsterInfo != null &&
                        monster._monsterInfo.group_id == group_id &&
                        monster._monsterInfo.config_id == config_id)
            .Cast<MonsterEntity>()
            .ToList();
        foreach (var monster in monsters)
        {
            player.Session.SendPacket(new MonsterForceAlertNotify()
            {
                MonsterEntityId = monster._EntityId,
                IsAlert = is_alert != 0
			});
		}
        return 0;
	}

	public int BeginCameraSceneLook(Session session, object _lookInfo)
    {
        Log("Called BeginCameraSceneLook");
        // todo
        return 0;
    }

    public int SetWorktopOptions(Session session, object optionTable)
    {
        Log("Called SetWorktopOptions");

        if (currentEventArgs == null)
        {
            currentSession.c.LogWarning("[ScriptLib] SetWorktopOptions called without current event args");
            return -1;
        }

        int groupId = currentEventArgs.group_id;
        int configId = currentEventArgs.param1;

        return SetWorktopOptionsByGroupId(session, groupId, configId, optionTable);
    }

    public int SetWorktopOptionsByGroupId(Session session, int group_id, int config_id, object optionTable)
    {
        Log("Called SetWorktopOptionsByGroupId");

        if (currentSession.player?.Scene == null)
            return -1;

        LuaTable? table = optionTable as LuaTable;
        if (table == null)
            return -1;

        List<uint> options = new();
        foreach (var value in table.Values)
        {
            try
            {
                uint opt = Convert.ToUInt32(value);
                if (opt != 0 && !options.Contains(opt))
                    options.Add(opt);
            }
            catch
            {
                // ignore invalid entries
            }
        }

        if (options.Count == 0)
            return -1;

        var scene = currentSession.player.Scene;
        var gadgets = scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity g &&
                        g._gadgetLua != null &&
                        g._gadgetLua.group_id == group_id &&
                        g._gadgetLua.config_id == config_id)
            .Cast<GadgetEntity>()
            .ToList();

        if (gadgets.Count == 0)
            return -1;

        foreach (var gadget in gadgets)
        {
            gadget.WorktopOptions.Clear();
            foreach (var opt in options)
                gadget.WorktopOptions.Add(opt);

            var notify = new WorktopOptionNotify
            {
                GadgetEntityId = gadget._EntityId
            };

            foreach (var opt in gadget.WorktopOptions.OrderBy(o => o))
                notify.OptionLists.Add(opt);

            currentSession.SendPacket(notify);
        }

        return 0;
    }

    public int DelWorktopOption(Session session, int option_id)
    {
        Log("Called DelWorktopOption");

        if (currentEventArgs == null)
        {
            currentSession.c.LogWarning("[ScriptLib] DelWorktopOption called without current event args");
            return -1;
        }

        int groupId = currentEventArgs.group_id;
        int configId = currentEventArgs.param1;

        return DelWorktopOptionByGroupId(session, groupId, configId, option_id);
    }

    public int DelWorktopOptionByGroupId(Session session, int group_id, int config_id, int option_id)
    {
        Log("Called DelWorktopOptionByGroupId");

        if (currentSession.player?.Scene == null)
            return -1;

        uint opt = (uint)option_id;

        var scene = currentSession.player.Scene;
        var gadgets = scene.EntityManager.Entities.Values
            .Where(e => e is GadgetEntity g &&
                        g._gadgetLua != null &&
                        g._gadgetLua.group_id == group_id &&
                        g._gadgetLua.config_id == config_id)
            .Cast<GadgetEntity>()
            .ToList();

        if (gadgets.Count == 0)
            return -1;

        foreach (var gadget in gadgets)
        {
            if (gadget.WorktopOptions.Remove(opt))
            {
                var notify = new WorktopOptionNotify
                {
                    GadgetEntityId = gadget._EntityId
                };

                foreach (var remaining in gadget.WorktopOptions.OrderBy(o => o))
                    notify.OptionLists.Add(remaining);

                currentSession.SendPacket(notify);
            }
        }

        return 0;
    }

    public ScriptLib(Session session)
    {
        currentSession = session;
    }

    private void Log(string msg)
    {
        currentSession.c.LogWarning($"[ScriptLib] {msg}");
    }
}
