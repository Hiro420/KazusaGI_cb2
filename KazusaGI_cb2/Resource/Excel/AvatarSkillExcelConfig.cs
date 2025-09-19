using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class AvatarSkillExcelConfig
{
    public uint id;
    public uint nameTextMapHash;
    public string abilityName;
    public uint descTextMapHash;
    public ElementType costElemType;
    public int costElemVal;
	public float cdTime;
    public float costStamina;
    public int maxChargeNum;
    public int triggerID;
    public bool defaultLocked;
    public uint proudSkillGroupId;
}