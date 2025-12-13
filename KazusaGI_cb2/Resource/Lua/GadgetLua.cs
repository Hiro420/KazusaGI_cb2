using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class GadgetLua
{
    public uint config_id;
    public uint gadget_id;
    public Vector3 pos;
    public Vector3 rot;
    // state
    public uint route_id;
    // persistent
    public uint level;

    // custom fields
    public uint block_id;
    public uint group_id;

    public GadgetState state;
    public GadgetType_Lua type;

    // How this gadget is considered born by the client.
    // Mirrors proto::GadgetBornType / GadgetBornType in hk4e.
    public KazusaGI_cb2.Protocol.GadgetBornType born_type;

    // Script-level flags/fields, mirroring hk4e's GadgetScriptConfig
    // These are taken directly from group gadget Lua tables.
    public bool isOneoff;        // isOneoff = true/false
    public bool persistent;      // persistent = true/false
    public bool showcutscene;    // showcutscene = true/false
    public string? drop_tag;     // drop_tag = "..." (may be null/empty)

    // Gathering/point data
    public uint point_type;      // point_type = <uint>, 0 if not present

    // Owner gadget config_id for attached gadgets (e.g. gather points).
    // Mirrors owner field in group gadget Lua.
    public uint owner;           // owner = <config_id>, 0 if none
}