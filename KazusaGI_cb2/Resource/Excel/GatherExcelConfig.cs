using System.Collections.Generic;

namespace KazusaGI_cb2.Resource.Excel;

public class GatherExcelConfig
{
    public uint id { get; set; }
    public uint pointType { get; set; }
    public uint gadgetId { get; set; }
    public uint itemId { get; set; }
    public List<uint> extraItemIdVec { get; set; } = new();
    public uint cd { get; set; }
    public uint priority { get; set; }
    public uint refreshId { get; set; }

    public class BlockLimit
    {
        public uint blockId { get; set; }
        public uint count { get; set; }
    }

    public List<BlockLimit> blockLimits { get; set; } = new();
}
