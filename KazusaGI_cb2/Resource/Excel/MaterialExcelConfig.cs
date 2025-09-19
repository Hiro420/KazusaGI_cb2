using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class MaterialExcelConfig
{
    public MaterialType materialType;
    public uint cdTime;
    public uint cdGroup;
    public uint stackLimit;
    public uint maxUseCount;
    public bool useOnGain;
    public uint effectGadgetID;
    public uint id;
    public uint nameTextMapHash;
    public ItemType itemType;
    public uint gadgetId;
}