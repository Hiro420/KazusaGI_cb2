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
    public static Entity? targetEntity;

    public int GetGroupMonsterCount(Session session)
    {
        Log("Called GetGroupMonsterCount");
        return currentSession.entityMap.Values
            .Where(e => e is MonsterEntity monster &&
                        monster._monsterInfo != null &&
                        monster._monsterInfo.group_id == currentGroupId)
            .Count();
    }

    public int SetGadgetState(Session session, int gadgetState)
    {
        if (targetEntity == null || targetEntity is not GadgetEntity)
        {
            Log("SetGadgetState failed: targetEntity is null or not a GadgetEntity");
            return -1;
        }
        Log("Called SetGadgetState");
        GadgetEntity gadget = (GadgetEntity)targetEntity;
        gadget.ChangeState((GadgetState)gadgetState);
        targetEntity = null;
        return 0;
    }

    public int GetGroupMonsterCountByGroupId(Session session, int groupId)
    {
        Log("Called GetGroupMonsterCountByGroupId");
        return currentSession.entityMap.Values
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
        IEnumerable<Entity> entities = currentSession.entityMap.Values
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
        IEnumerable<Entity> entities = currentSession.entityMap.Values
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
        GadgetEntity? gadget = currentSession.entityMap.Values
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

    public int BeginCameraSceneLook(Session session, object _lookInfo)
    {
        Log("Called BeginCameraSceneLook");
        // todo
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
