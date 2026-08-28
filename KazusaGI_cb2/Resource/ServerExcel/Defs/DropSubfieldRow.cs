namespace KazusaGI_cb2.Resource.ServerExcel;

public class DropSubfieldRow
{
	[TsvColumn("分支掉落池id")]
	public int SubfieldPoolId { get; set; }

	[TsvColumn("等级上限")]
	public int LevelMax { get; set; }

	[TsvColumn("掉落id")]
	public int DropId { get; set; }

	[TsvColumn("产出来源类型")]
	public int ProduceSourceType { get; set; }
}
