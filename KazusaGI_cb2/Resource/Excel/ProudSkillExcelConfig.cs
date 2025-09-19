using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class ProudSkillExcelConfig
{
    public uint proudSkillId; // 0x28
    public uint proudSkillGroupId; // 0x2C
    public uint level; // 0x30
    public uint proudSkillType; // 0x34
    internal uint nameTextMapHash; // 0x38
    internal uint descTextMapHash; // 0x3C
    internal uint unlockDescTextMapHash; // 0x40
    public string icon; // 0x48
    public string openConfig;
	public uint coinCost; // 0x50
    public uint breakLevel; // 0x68
    internal List<uint> paramDescList; // 0x70
    public List<string> lifeEffectParams; // 0x80
    public uint FKJBDHCBPJA; // 0x88
    public double[] paramList;

	// props later
}