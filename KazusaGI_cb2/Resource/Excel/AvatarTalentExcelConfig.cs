using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public partial class AvatarTalentExcelConfig
{
	public uint talentId { get; set; }
	public long nameTextMapHash { get; set; }
	public long descTextMapHash { get; set; }
	public string icon { get; set; }
	public uint mainCostItemId { get; set; }
	public uint mainCostItemCount { get; set; }
	public string openConfig { get; set; }
	public List<AddProp> addProps { get; set; }
	public List<double> paramList { get; set; }
	public uint? prevTalent { get; set; }
}

public partial class AddProp
{
	public string propType { get; set; }
	public double? value { get; set; }
}