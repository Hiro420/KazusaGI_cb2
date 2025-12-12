using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KazusaGI_cb2.Resource.Misc;

namespace KazusaGI_cb2.Resource.Excel;

public class WeaponPromoteExcelConfig
{
	public uint weaponPromoteId;
	public List<CostItem> costItems;
	public List<AddProp> addProps;
	public uint unlockMaxLevel;
	public uint promoteLevel = 0;
	public uint coinCost = 0;
	public uint requiredPlayerLevel = 0;
}
