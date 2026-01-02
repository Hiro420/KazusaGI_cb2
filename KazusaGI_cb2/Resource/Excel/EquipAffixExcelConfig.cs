using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class EquipAffixExcelConfig
{
	public uint AffixId;
	public uint Id;
	public uint Level;
	public uint NameTextMapHash;
	public uint DescTextMapHash;
	public string? OpenConfig;
	public List<AddProp> AddProps = new();
	public List<double> ParamList = new();
}