using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class MonsterDropRow
{
    [TsvColumn("最小等级")]
    public int MinLevel { get; set; }

    [TsvColumn("总索引")]
    public string GroupKey { get; set; } = string.Empty;

    [TsvColumn("掉落ID")]
    public int DropId { get; set; }

    [TsvColumn("掉落次数")]
    public int DropCount { get; set; }
}
