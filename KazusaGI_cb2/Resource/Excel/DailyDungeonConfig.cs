﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class DailyDungeonConfig
{
    public uint id;
    public List<uint> monday;
    public List<uint> tuesday;
    public List<uint> wednesday;
    public List<uint> thursday;
    public List<uint> friday;
    public List<uint> saturday;
    public List<uint> sunday;
}