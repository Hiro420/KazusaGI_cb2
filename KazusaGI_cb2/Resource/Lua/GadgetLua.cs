﻿using System;
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
}