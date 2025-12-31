using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class MonsterAffixRow
{
    [TsvColumn("ID")]
    public int Id { get; set; }

    [TsvColumn("说明", Required = false)]
    public string? Description { get; set; }

    [TsvColumn("AbilityName")]
    public string AbilityName { get; set; } = string.Empty;

    [TsvColumn("是否通用", Required = false)]
    public int? IsCommon { get; set; }

    [TsvColumn("最先加载", Required = false)]
    public int? LoadFirst { get; set; }

    [TsvColumn("图标", Required = false)]
    public string? Icon { get; set; }

    [TsvColumn("$#VersionBegin", Required = false)]
    public decimal? VersionBegin { get; set; }

    [TsvColumn("$#VersionEnd", Required = false)]
    public decimal? VersionEnd { get; set; }
}
