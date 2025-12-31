using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using KazusaGI_cb2.GameServer.Lua;

namespace KazusaGI_cb2.Resource;

public class SceneTriggerLua
{
    public string name;
    public EventType _event;
    public string source;
    public string condition;
    public string action;
    // Lua field: trigger_count. In hk4e this is used as a
    // "max trigger count" cap: 0 means unlimited, any positive
    // value is the maximum number of times this trigger is allowed
    // to fire for a given group.
    public uint trigger_count;
}
