using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class ChestDropRow
{
	[TsvColumn("最小等级")]
	public int MinLevel { get; set; }

	[TsvColumn("总索引")]
	public string DropTag { get; set; } = string.Empty;

	[TsvColumn("掉落ID")]
	public int DropId { get; set; }

	[TsvColumn("掉落次数")]
	public int DropCount { get; set; }

	[TsvColumn("产出来源类型")]
	public int ProduceSourceType { get; set; }
}
