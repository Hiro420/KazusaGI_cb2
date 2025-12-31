using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class AvatarRow
{
    [TsvColumn("ID")]
    public int Id { get; set; }

    [TsvColumn("默认阵营")]
    public int? DefaultCampId { get; set; }

    [TsvColumn("基础生命值")]
    public decimal BaseHp { get; set; }

    [TsvColumn("基础攻击力")]
    public decimal BaseAttack { get; set; }

    [TsvColumn("基础防御力")]
    public decimal BaseDefense { get; set; }

    [TsvColumn("暴击率")]
    public decimal CritRate { get; set; }

    [TsvColumn("暴击抗性")]
    public decimal CritResist { get; set; }

    [TsvColumn("暴击伤害")]
    public decimal CritDamage { get; set; }

    [TsvColumn("火元素抗性")]
    public decimal FireResist { get; set; }

    [TsvColumn("草元素抗性")]
    public decimal GrassResist { get; set; }

    [TsvColumn("水元素抗性")]
    public decimal WaterResist { get; set; }

    [TsvColumn("电元素抗性")]
    public decimal ElectricResist { get; set; }

    [TsvColumn("风元素抗性")]
    public decimal WindResist { get; set; }

    [TsvColumn("冰元素抗性")]
    public decimal IceResist { get; set; }

    [TsvColumn("岩元素抗性")]
    public decimal RockResist { get; set; }

    [TsvColumn("火元素伤害加成")]
    public decimal FireDamageBonus { get; set; }

    [TsvColumn("草元素伤害加成")]
    public decimal GrassDamageBonus { get; set; }

    [TsvColumn("水元素伤害加成")]
    public decimal WaterDamageBonus { get; set; }

    [TsvColumn("电元素伤害加成")]
    public decimal ElectricDamageBonus { get; set; }

    [TsvColumn("风元素伤害加成")]
    public decimal WindDamageBonus { get; set; }

    [TsvColumn("冰元素伤害加成")]
    public decimal IceDamageBonus { get; set; }

    [TsvColumn("岩元素伤害加成")]
    public decimal RockDamageBonus { get; set; }

    [TsvColumn("[属性成长]1类型")]
    public int Growth1Type { get; set; }

    [TsvColumn("[属性成长]1曲线")]
    public int Growth1Curve { get; set; }

    [TsvColumn("[属性成长]2类型")]
    public int Growth2Type { get; set; }

    [TsvColumn("[属性成长]2曲线")]
    public int Growth2Curve { get; set; }

    [TsvColumn("[属性成长]3类型")]
    public int Growth3Type { get; set; }

    [TsvColumn("[属性成长]3曲线")]
    public int Growth3Curve { get; set; }

    [TsvColumn("元素精通")]
    public int ElementMastery { get; set; }

    [TsvColumn("物理抗性")]
    public decimal PhysicalResist { get; set; }

    [TsvColumn("物理伤害加成")]
    public decimal PhysicalDamageBonus { get; set; }

    [TsvColumn("是否使用")]
    public int IsUsed { get; set; }

    [TsvColumn("角色品质")]
    public int Quality { get; set; }

    [TsvColumn("充能效率")]
    public decimal EnergyRecharge { get; set; }

    [TsvColumn("治疗效果")]
    public decimal HealingBonus { get; set; }

    [TsvColumn("受治疗效果")]
    public decimal IncomingHealingBonus { get; set; }

    [TsvColumn("战斗config")]
    public string CombatConfig { get; set; } = string.Empty;

    [TsvColumn("是否远程射击角色")]
    public int IsRangedShooter { get; set; }

    [TsvColumn("初始武器")]
    public int InitialWeaponId { get; set; }

    [TsvColumn("武器种类")]
    public int WeaponType { get; set; }

    [TsvColumn("技能库ID")]
    public int SkillDepotId { get; set; }

    [TsvColumn("体力恢复速度")]
    public int StaminaRecoverSpeed { get; set; }

    [TsvColumn("候选技能库ID")]
    public string? CandidateSkillDepotIds { get; set; }

    [TsvColumn("角色类型")]
    public int AvatarType { get; set; }

    [TsvColumn("羁绊组ID")]
    public int FetterGroupId { get; set; }

    [TsvColumn("角色突破ID")]
    public int AvatarPromoteId { get; set; }

    [TsvColumn("生日[月]")]
    public int BirthdayMonth { get; set; }

    [TsvColumn("生日[日]")]
    public int BirthdayDay { get; set; }

    [TsvColumn("特性组ID")]
    public int FeatureGroupId { get; set; }

    [TsvColumn("名称$text_name_Name")]
    public string NameTextMap { get; set; } = string.Empty;

    [TsvColumn("出生地$text_name_Native")]
    public string NativeTextMap { get; set; } = string.Empty;

    [TsvColumn("神之眼$text_name_Vision")]
    public string VisionTextMap { get; set; } = string.Empty;

    [TsvColumn("命之座$text_name_Constellation")]
    public string ConstellationTextMap { get; set; } = string.Empty;

    [TsvColumn("描述$text_name_Desc")]
    public string DescriptionTextMap { get; set; } = string.Empty;

    [TsvColumn("$#VersionBegin", Required = false)]
    public decimal? VersionBegin { get; set; }

    [TsvColumn("$#VersionEnd", Required = false)]
    public decimal? VersionEnd { get; set; }
}
