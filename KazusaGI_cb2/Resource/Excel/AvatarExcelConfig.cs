using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.Resource.Excel;

public class AvatarExcelConfig
{
    public AvatarUseType useType;
    public BodyType bodyType;
    public QualityType qualityType;
    public string iconName;
	public float chargeEfficiency;
    public uint initialWeapon;
    public WeaponType weaponType;
    public uint skillDepotId;
    public uint staminaRecoverSpeed;
    public List<uint> candSkillDepotIds;
    public uint descTextMapHash;
    public AvatarIdentityType avatarIdentityType;
    public uint avatarPromoteId;

    public float hpBase;
    public float attackBase;
    public float defenseBase;
    public float critical;
    public float criticalHurt;

    public List<FightPropGrowConfig> propGrowCurves;

    public uint id;
    public uint nameTextMapHash;
}