using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class ReliquaryAffixExcelConfig
{
	public uint id;
	public uint depotId;
	public uint groupId;
	public FightPropType propType;
	public double propValue;
	public uint weight;
	public uint upgradeWeight;
}