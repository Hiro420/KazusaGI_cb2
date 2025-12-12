using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
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
                        monster._monsterInfo != null &&
                        monster._monsterInfo.group_id == currentGroupId)
            .Count();
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
        if (currentEventArgs != null)
        {
            var args = new ScriptArgs(currentGroupId, (int)TriggerEventType.EVENT_VARIABLE_CHANGE)
            {
                param1 = oldValue,
                param2 = value,
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

        if (currentEventArgs != null)
        {
            var args = new ScriptArgs(currentGroupId, (int)TriggerEventType.EVENT_VARIABLE_CHANGE)
            {
                param1 = oldValue,
                param2 = newValue,
                source = var_name
            };

            LuaManager.executeTriggersLua(currentSession, group, args);
        }

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
        int groupId = (int)(long)groupInfo!["group_id"];
        int suite = (int)(long)groupInfo["suite"];
        SceneGroupLua? group = currentSession.player!.Scene.GetGroup(groupId);
        if (group == null)
            return -1;
        currentSession.player!.Scene.RefreshGroup(group, suite);
        return 0;
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

        int configId;
        try
        {
            configId = (int)(long)config["config_id"];
        }
        catch
        {
            return -1;
        }

        int delayTime = 0;
        try
        {
            var delayObj = config["delay_time"];
            if (delayObj != null)
            {
                delayTime = (int)(long)delayObj;
            }
        }
        catch
        {
            // ignore missing/invalid delay_time; spawn immediately
        }

        if (delayTime > 0)
        {
            currentSession.c.LogWarning($"[ScriptLib] CreateMonster delay_time={delayTime} currently ignored (spawning immediately)");
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
            return -1;

        var monsterInfo = group.monsters.FirstOrDefault(m => m.config_id == (uint)configId);
        if (monsterInfo == null)
            return -1;

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

        var args = new ScriptArgs(groupId, (int)TriggerEventType.EVENT_ANY_MONSTER_LIVE, (int)monsterInfo.config_id);
        LuaManager.executeTriggersLua(currentSession, group, args);

        return 0;
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

        var args = new ScriptArgs(groupId, (int)TriggerEventType.EVENT_GADGET_CREATE, (int)gadgetInfo.config_id);
        LuaManager.executeTriggersLua(currentSession, group, args);

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
