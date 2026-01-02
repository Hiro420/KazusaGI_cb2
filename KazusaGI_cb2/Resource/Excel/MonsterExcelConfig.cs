using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class MonsterExcelConfig
{
    public string monsterName;
    public MonsterType type;
    public string serverScript;
    public List<uint> affix;
    public string ai;
    public List<uint> equips;
    // killExpCurve
    public List<HpDrop> hpDrops;
    public uint killDropId;
    // excludeWeathers
    public uint featureTagGroupID;
    public uint mpPropID;
    public float hpBase;
    public float attackBase;
    public float defenseBase;
    public List<FightPropGrowConfig> propGrowCurves;
    public uint id;
    public uint campID;
}