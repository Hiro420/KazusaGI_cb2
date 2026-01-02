using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class EntityDropSubfieldRow
{
	[TsvColumn("EntityId")]
	public int EntityId { get; set; }

	[TsvColumn("类型")]
	public int Type { get; set; }

	// ===== 分支掉落 1 =====

	[TsvColumn("[分支掉落]1类型", Required = false)]
	public string? Branch1Type { get; set; }

	[TsvColumn("[分支掉落]1分支掉落池id", Required = false)]
	public int? Branch1PoolId { get; set; }

	// ===== 分支掉落 2 =====

	[TsvColumn("[分支掉落]2类型", Required = false)]
	public string? Branch2Type { get; set; }

	[TsvColumn("[分支掉落]2分支掉落池id", Required = false)]
	public int? Branch2PoolId { get; set; }

	// ===== 分支掉落 3 =====

	[TsvColumn("[分支掉落]3类型", Required = false)]
	public string? Branch3Type { get; set; }

	[TsvColumn("[分支掉落]3分支掉落池id", Required = false)]
	public int? Branch3PoolId { get; set; }

	// ===== 分支掉落 4 =====

	[TsvColumn("[分支掉落]4类型", Required = false)]
	public string? Branch4Type { get; set; }

	[TsvColumn("[分支掉落]4分支掉落池id", Required = false)]
	public int? Branch4PoolId { get; set; }

	// ===== 分支掉落 5 =====

	[TsvColumn("[分支掉落]5类型", Required = false)]
	public string? Branch5Type { get; set; }

	[TsvColumn("[分支掉落]5分支掉落池id", Required = false)]
	public int? Branch5PoolId { get; set; }

	// ===== 分支掉落 6 =====

	[TsvColumn("[分支掉落]6类型", Required = false)]
	public string? Branch6Type { get; set; }

	[TsvColumn("[分支掉落]6分支掉落池id", Required = false)]
	public int? Branch6PoolId { get; set; }

	// ===== 分支掉落 7 =====

	[TsvColumn("[分支掉落]7类型", Required = false)]
	public string? Branch7Type { get; set; }

	[TsvColumn("[分支掉落]7分支掉落池id", Required = false)]
	public int? Branch7PoolId { get; set; }

	// ===== Limits =====

	[TsvColumn("掉落上限", Required = false)]
	public int? DropLimit { get; set; }

	[TsvColumn("每日掉落上限", Required = false)]
	public int? DailyDropLimit { get; set; }
}
