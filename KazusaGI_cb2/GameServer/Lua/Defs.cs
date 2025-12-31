using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Lua;

public class Variable
{
    public string name;
    public int value;
}

public class ScriptArgs
{
    public int param1;
    public int param2;
    public int param3;
    public int source_eid; // Source entity
    public int target_eid;
    public int group_id;
    public string source; // source string, used for timers
    public int type;

    public EventType eventTypeAsEnum()
    {
        return (EventType)type;
    }

    public ScriptArgs(int groupId, int eventType, int param1, int param2)
    {
        this.group_id = groupId;
        this.type = eventType;
        this.param1 = param1;
        this.param2 = param2;
    }
    public ScriptArgs(int groupId, int eventType)
    {
        this.group_id = groupId;
        this.type = eventType;
    }
    public ScriptArgs(int groupId, int eventType, int param1)
    {
        this.group_id = groupId;
        this.type = eventType;
        this.param1 = param1;
    }

    public object toTable()
    {
        return new { param1 = param1, param2 = param2, param3 = param3, source_eid = source_eid, target_eid = target_eid, type = type, group_id = group_id, source = source };
    }
}

public enum EventType
{
	EVENT_NONE = 0x0,
	EVENT_ANY_MONSTER_DIE = 0x1,
	EVENT_ANY_GADGET_DIE = 0x2,
	EVENT_VARIABLE_CHANGE = 0x3,
	EVENT_ENTER_REGION = 0x4,
	EVENT_LEAVE_REGION = 0x5,
	EVENT_GADGET_CREATE = 0x6,
	EVENT_GADGET_STATE_CHANGE = 0x7,
	EVENT_DUNGEON_SETTLE = 0x8,
	EVENT_SELECT_OPTION = 0x9,
	EVENT_CLIENT_EXECUTE = 0xA,
	EVENT_ANY_MONSTER_LIVE = 0xB,
	EVENT_SPECIFIC_MONSTER_HP_CHANGE = 0xC,
	EVENT_CITY_LEVELUP_UNLOCK_DUNGEON_ENTRY = 0xD,
	EVENT_DUNGEON_BROADCAST_ONTIMER = 0xE,
	EVENT_TIMER_EVENT = 0xF,
	EVENT_CHALLENGE_SUCCESS = 0x10,
	EVENT_CHALLENGE_FAIL = 0x11,
	EVENT_SEAL_BATTLE_BEGIN = 0x12,
	EVENT_SEAL_BATTLE_END = 0x13,
	EVENT_GATHER = 0x14,
	EVENT_QUEST_FINISH = 0x15,
	EVENT_MONSTER_BATTLE = 0x16,
	EVENT_CITY_LEVELUP = 0x17,
	EVENT_CUTSCENE_END = 0x18,
	EVENT_AVATAR_NEAR_PLATFORM = 0x19,
	EVENT_PLATFORM_REACH_POINT = 0x1A,
	EVENT_UNLOCK_TRANS_POINT = 0x1B,
	EVENT_QUEST_START = 0x1C,
	EVENT_GROUP_LOAD = 0x1D,
	EVENT_GROUP_REFRESH = 0x1E,
	EVENT_DUNGEON_REWARD_GET = 0x1F,
	EVENT_SPECIFIC_GADGET_HP_CHANGE = 0x20,
	EVENT_MONSTER_TIDE_OVER = 0x21,
	EVENT_MONSTER_TIDE_CREATE = 0x22,
	EVENT_MONSTER_TIDE_DIE = 0x23,
	EVENT_SEALAMP_PHASE_CHANGE = 0x24,
}