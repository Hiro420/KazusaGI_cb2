using System;

namespace KazusaGI_cb2.Resource.ServerExcel;

public sealed class MonsterRow
{
    [TsvColumn("ID")]
    public int Id { get; set; }

    [TsvColumn("默认阵营")]
    public int DefaultCampId { get; set; }

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

    [TsvColumn("类型")]
    public int MonsterType { get; set; }

    [TsvColumn("服务器脚本", Required = false)]
    public string? ServerScript { get; set; }

    [TsvColumn("战斗Config", Required = false)]
    public string? CombatConfig { get; set; }

    [TsvColumn("精英词缀", Required = false)]
    public string? EliteAffixes { get; set; }

    [TsvColumn("AI", Required = false)]
    public string? AiConfig { get; set; }

    [TsvColumn("装备1", Required = false)]
    public int? Equip1 { get; set; }

    [TsvColumn("装备2", Required = false)]
    public int? Equip2 { get; set; }

    [TsvColumn("会游泳", Required = false)]
    public int? CanSwim { get; set; }

    [TsvColumn("击杀经验")]
    public int KillExp { get; set; }

    [TsvColumn("击杀经验成长曲线")]
    public int KillExpCurve { get; set; }

    [TsvColumn("[掉落]1ID", Required = false)]
    public int? Drop1Id { get; set; }

    [TsvColumn("[掉落]1血量百分比", Required = false)]
    public int? Drop1HpPercent { get; set; }

    [TsvColumn("[掉落]2ID", Required = false)]
    public int? Drop2Id { get; set; }

    [TsvColumn("[掉落]2血量百分比", Required = false)]
    public int? Drop2HpPercent { get; set; }

    [TsvColumn("[掉落]3ID", Required = false)]
    public int? Drop3Id { get; set; }

    [TsvColumn("[掉落]3血量百分比", Required = false)]
    public int? Drop3HpPercent { get; set; }

    [TsvColumn("击杀掉落ID", Required = false)]
    public int? KillDropId { get; set; }

    [TsvColumn("全地图奖励", Required = false)]
    public int? GlobalReward { get; set; }

    [TsvColumn("视距等级", Required = false)]
    public int? ViewDistanceLevel { get; set; }

    [TsvColumn("不在指定天气下出生", Required = false)]
    public int? NotSpawnInSpecificWeather { get; set; }

    [TsvColumn("特性组ID", Required = false)]
    public int? FeatureGroupId { get; set; }

    [TsvColumn("联机动态属性ID", Required = false)]
    public int? CoopDynamicPropertyId { get; set; }

    [TsvColumn("外观", Required = false)]
    public int? AppearanceId { get; set; }

    [TsvColumn("$#VersionBegin", Required = false)]
    public decimal? VersionBegin { get; set; }

    [TsvColumn("$#VersionEnd", Required = false)]
    public decimal? VersionEnd { get; set; }
}
