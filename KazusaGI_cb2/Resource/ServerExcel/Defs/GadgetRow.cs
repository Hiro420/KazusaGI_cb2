using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class GadgetRow
{
    [TsvColumn("ID")]
    public int Id { get; set; }

    [TsvColumn("默认阵营")]
    public int DefaultCampId { get; set; }

    [TsvColumn("类型")]
    public int Type { get; set; }

    [TsvColumn("JSON名称", Required = false)]
    public string? JsonName { get; set; }

    [TsvColumn("能否移动", Required = false)]
    public int? CanMove { get; set; }

    [TsvColumn("是否有音响", Required = false)]
    public int? HasAudio { get; set; }

    [TsvColumn("是否能装备", Required = false)]
    public int? CanEquip { get; set; }

    [TsvColumn("能否交互", Required = false)]
    public int? CanInteract { get; set; }

    [TsvColumn("视距等级", Required = false)]
    public int? ViewDistanceLevel { get; set; }

    [TsvColumn("服务器脚本", Required = false)]
    public string? ServerScript { get; set; }

    [TsvColumn("ItemJSON名称", Required = false)]
    public string? ItemJsonName { get; set; }

    [TsvColumn("$#VersionBegin", Required = false)]
    public decimal? VersionBegin { get; set; }

    [TsvColumn("$#VersionEnd", Required = false)]
    public decimal? VersionEnd { get; set; }
}
