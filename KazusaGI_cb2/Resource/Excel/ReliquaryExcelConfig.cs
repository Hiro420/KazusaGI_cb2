using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class ReliquaryExcelConfig
{
	public EquipType equipType;
	public string showPic;
	public uint rankLevel;
	public uint mainPropDepotId;
	public uint appendPropDepotId;
	public uint appendPropNum;
	public uint setId;
	public List<uint> addPropLevels;
	public uint baseConvExp;
	public uint maxLevel;
	public uint storyId;
	public uint id;
	public uint nameTextMapHash;
	public long descTextMapHash;
	public string icon;
	public ItemType itemType;
	public uint weight;
	public uint rank;
	public uint gadgetId;
}