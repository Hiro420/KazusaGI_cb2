using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class DropTreeRow
{
	[TsvColumn("掉落ID")]
	public int Id { get; set; }

	[TsvColumn("最小等级", Required = false)]
	public int? MinLevel { get; set; }

	[TsvColumn("最大等级", Required = false)]
	public int? MaxLevel { get; set; }

	[TsvColumn("随机方式", Required = false)]
	public int? RandomType { get; set; }

	[TsvColumn("掉落层级", Required = false)]
	public decimal? DropLevel { get; set; }

	// ===== 子掉落 1 ~ 30 =====

	[TsvColumn("子掉落1ID", Required = false)]
	public int? SubDrop1Id { get; set; }
	[TsvColumn("子掉落1数量区间", Required = false)]
	public string? SubDrop1CountRange { get; set; }
	[TsvColumn("子掉落1权重", Required = false)]
	public int? SubDrop1Weight { get; set; }

	[TsvColumn("子掉落2ID", Required = false)]
	public int? SubDrop2Id { get; set; }
	[TsvColumn("子掉落2数量区间", Required = false)]
	public string? SubDrop2CountRange { get; set; }
	[TsvColumn("子掉落2权重", Required = false)]
	public int? SubDrop2Weight { get; set; }

	[TsvColumn("子掉落3ID", Required = false)]
	public int? SubDrop3Id { get; set; }
	[TsvColumn("子掉落3数量区间", Required = false)]
	public string? SubDrop3CountRange { get; set; }
	[TsvColumn("子掉落3权重", Required = false)]
	public int? SubDrop3Weight { get; set; }

	[TsvColumn("子掉落4ID", Required = false)]
	public int? SubDrop4Id { get; set; }
	[TsvColumn("子掉落4数量区间", Required = false)]
	public string? SubDrop4CountRange { get; set; }
	[TsvColumn("子掉落4权重", Required = false)]
	public int? SubDrop4Weight { get; set; }

	[TsvColumn("子掉落5ID", Required = false)]
	public int? SubDrop5Id { get; set; }
	[TsvColumn("子掉落5数量区间", Required = false)]
	public string? SubDrop5CountRange { get; set; }
	[TsvColumn("子掉落5权重", Required = false)]
	public int? SubDrop5Weight { get; set; }

	[TsvColumn("子掉落6ID", Required = false)]
	public int? SubDrop6Id { get; set; }
	[TsvColumn("子掉落6数量区间", Required = false)]
	public string? SubDrop6CountRange { get; set; }
	[TsvColumn("子掉落6权重", Required = false)]
	public int? SubDrop6Weight { get; set; }

	[TsvColumn("子掉落7ID", Required = false)]
	public int? SubDrop7Id { get; set; }
	[TsvColumn("子掉落7数量区间", Required = false)]
	public string? SubDrop7CountRange { get; set; }
	[TsvColumn("子掉落7权重", Required = false)]
	public int? SubDrop7Weight { get; set; }

	[TsvColumn("子掉落8ID", Required = false)]
	public int? SubDrop8Id { get; set; }
	[TsvColumn("子掉落8数量区间", Required = false)]
	public string? SubDrop8CountRange { get; set; }
	[TsvColumn("子掉落8权重", Required = false)]
	public int? SubDrop8Weight { get; set; }

	[TsvColumn("子掉落9ID", Required = false)]
	public int? SubDrop9Id { get; set; }
	[TsvColumn("子掉落9数量区间", Required = false)]
	public string? SubDrop9CountRange { get; set; }
	[TsvColumn("子掉落9权重", Required = false)]
	public int? SubDrop9Weight { get; set; }

	[TsvColumn("子掉落10ID", Required = false)]
	public int? SubDrop10Id { get; set; }
	[TsvColumn("子掉落10数量区间", Required = false)]
	public string? SubDrop10CountRange { get; set; }
	[TsvColumn("子掉落10权重", Required = false)]
	public int? SubDrop10Weight { get; set; }

	[TsvColumn("子掉落11ID", Required = false)]
	public int? SubDrop11Id { get; set; }
	[TsvColumn("子掉落11数量区间", Required = false)]
	public string? SubDrop11CountRange { get; set; }
	[TsvColumn("子掉落11权重", Required = false)]
	public int? SubDrop11Weight { get; set; }

	[TsvColumn("子掉落12ID", Required = false)]
	public int? SubDrop12Id { get; set; }
	[TsvColumn("子掉落12数量区间", Required = false)]
	public string? SubDrop12CountRange { get; set; }
	[TsvColumn("子掉落12权重", Required = false)]
	public int? SubDrop12Weight { get; set; }

	[TsvColumn("子掉落13ID", Required = false)]
	public int? SubDrop13Id { get; set; }
	[TsvColumn("子掉落13数量区间", Required = false)]
	public string? SubDrop13CountRange { get; set; }
	[TsvColumn("子掉落13权重", Required = false)]
	public int? SubDrop13Weight { get; set; }

	[TsvColumn("子掉落14ID", Required = false)]
	public int? SubDrop14Id { get; set; }
	[TsvColumn("子掉落14数量区间", Required = false)]
	public string? SubDrop14CountRange { get; set; }
	[TsvColumn("子掉落14权重", Required = false)]
	public int? SubDrop14Weight { get; set; }

	[TsvColumn("子掉落15ID", Required = false)]
	public int? SubDrop15Id { get; set; }
	[TsvColumn("子掉落15数量区间", Required = false)]
	public string? SubDrop15CountRange { get; set; }
	[TsvColumn("子掉落15权重", Required = false)]
	public int? SubDrop15Weight { get; set; }

	[TsvColumn("子掉落16ID", Required = false)]
	public int? SubDrop16Id { get; set; }
	[TsvColumn("子掉落16数量区间", Required = false)]
	public string? SubDrop16CountRange { get; set; }
	[TsvColumn("子掉落16权重", Required = false)]
	public int? SubDrop16Weight { get; set; }

	[TsvColumn("子掉落17ID", Required = false)]
	public int? SubDrop17Id { get; set; }
	[TsvColumn("子掉落17数量区间", Required = false)]
	public string? SubDrop17CountRange { get; set; }
	[TsvColumn("子掉落17权重", Required = false)]
	public int? SubDrop17Weight { get; set; }

	[TsvColumn("子掉落18ID", Required = false)]
	public int? SubDrop18Id { get; set; }
	[TsvColumn("子掉落18数量区间", Required = false)]
	public string? SubDrop18CountRange { get; set; }
	[TsvColumn("子掉落18权重", Required = false)]
	public int? SubDrop18Weight { get; set; }

	[TsvColumn("子掉落19ID", Required = false)]
	public int? SubDrop19Id { get; set; }
	[TsvColumn("子掉落19数量区间", Required = false)]
	public string? SubDrop19CountRange { get; set; }
	[TsvColumn("子掉落19权重", Required = false)]
	public int? SubDrop19Weight { get; set; }

	[TsvColumn("子掉落20ID", Required = false)]
	public int? SubDrop20Id { get; set; }
	[TsvColumn("子掉落20数量区间", Required = false)]
	public string? SubDrop20CountRange { get; set; }
	[TsvColumn("子掉落20权重", Required = false)]
	public int? SubDrop20Weight { get; set; }

	[TsvColumn("子掉落21ID", Required = false)]
	public int? SubDrop21Id { get; set; }
	[TsvColumn("子掉落21数量区间", Required = false)]
	public string? SubDrop21CountRange { get; set; }
	[TsvColumn("子掉落21权重", Required = false)]
	public int? SubDrop21Weight { get; set; }

	[TsvColumn("子掉落22ID", Required = false)]
	public int? SubDrop22Id { get; set; }
	[TsvColumn("子掉落22数量区间", Required = false)]
	public string? SubDrop22CountRange { get; set; }
	[TsvColumn("子掉落22权重", Required = false)]
	public int? SubDrop22Weight { get; set; }

	[TsvColumn("子掉落23ID", Required = false)]
	public int? SubDrop23Id { get; set; }
	[TsvColumn("子掉落23数量区间", Required = false)]
	public string? SubDrop23CountRange { get; set; }
	[TsvColumn("子掉落23权重", Required = false)]
	public int? SubDrop23Weight { get; set; }

	[TsvColumn("子掉落24ID", Required = false)]
	public int? SubDrop24Id { get; set; }
	[TsvColumn("子掉落24数量区间", Required = false)]
	public string? SubDrop24CountRange { get; set; }
	[TsvColumn("子掉落24权重", Required = false)]
	public int? SubDrop24Weight { get; set; }

	[TsvColumn("子掉落25ID", Required = false)]
	public int? SubDrop25Id { get; set; }
	[TsvColumn("子掉落25数量区间", Required = false)]
	public string? SubDrop25CountRange { get; set; }
	[TsvColumn("子掉落25权重", Required = false)]
	public int? SubDrop25Weight { get; set; }

	[TsvColumn("子掉落26ID", Required = false)]
	public int? SubDrop26Id { get; set; }
	[TsvColumn("子掉落26数量区间", Required = false)]
	public string? SubDrop26CountRange { get; set; }
	[TsvColumn("子掉落26权重", Required = false)]
	public int? SubDrop26Weight { get; set; }

	[TsvColumn("子掉落27ID", Required = false)]
	public int? SubDrop27Id { get; set; }
	[TsvColumn("子掉落27数量区间", Required = false)]
	public string? SubDrop27CountRange { get; set; }
	[TsvColumn("子掉落27权重", Required = false)]
	public int? SubDrop27Weight { get; set; }

	[TsvColumn("子掉落28ID", Required = false)]
	public int? SubDrop28Id { get; set; }
	[TsvColumn("子掉落28数量区间", Required = false)]
	public string? SubDrop28CountRange { get; set; }
	[TsvColumn("子掉落28权重", Required = false)]
	public int? SubDrop28Weight { get; set; }

	[TsvColumn("子掉落29ID", Required = false)]
	public int? SubDrop29Id { get; set; }
	[TsvColumn("子掉落29数量区间", Required = false)]
	public string? SubDrop29CountRange { get; set; }
	[TsvColumn("子掉落29权重", Required = false)]
	public int? SubDrop29Weight { get; set; }

	[TsvColumn("子掉落30ID", Required = false)]
	public int? SubDrop30Id { get; set; }
	[TsvColumn("子掉落30数量区间", Required = false)]
	public string? SubDrop30CountRange { get; set; }
	[TsvColumn("子掉落30权重", Required = false)]
	public int? SubDrop30Weight { get; set; }

	[TsvColumn("节点类型", Required = false)]
	public int? NodeType { get; set; }

	[TsvColumn("是否掉落地面", Required = false)]
	public int? IsDropOnGround { get; set; }

	[TsvColumn("产出来源类型", Required = false)]
	public int? ProduceSourceType { get; set; }

	[TsvColumn("每日产出次数上限", Required = false)]
	public int? DailyLimit { get; set; }

	[TsvColumn("历史产出次数上限", Required = false)]
	public int? TotalLimit { get; set; }
}
