using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KazusaGI_cb2.GameServer.PlayerInfos;
using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.GameServer.Ability;

public class ElementType
{
	[JsonProperty]
	public readonly Resource.ElementType Type;
	[JsonProperty]
	public readonly int TeamResonanceId;
	[JsonProperty]
	public readonly FightProp CurEnergyProp;
	public float CurEnergy = 0;
	[JsonProperty]
	public readonly FightProp MaxEnergyProp;
	public float MaxEnergy = 0;
	[JsonProperty]
	public readonly int DepotValue;
	[JsonProperty]
	public readonly int ConfigHash;
}

public class None : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.None;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_FIRE_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_FIRE_ENERGY;
}

public class Fire : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Fire;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10101;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_FIRE_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_FIRE_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 2;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Fire_Lv2");
}

public class Water : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Water;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10201;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_WATER_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_WATER_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 3;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Water_Lv2");
}
public class Wind : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Wind;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10301;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_WIND_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_WIND_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 4;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Wind_Lv2");
}
public class Ice : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Ice;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10601;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_ICE_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_ICE_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 5;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Ice_Lv2");
}
public class Rock : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Rock;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10701;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_ROCK_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_ROCK_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 6;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Rock_Lv2");
}

public class Electric : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Electric;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10401;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_ELEC_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_ELEC_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 7;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Electric_Lv2");
}
public class Grass : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Grass;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10501;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_GRASS_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_GRASS_ENERGY;
	[JsonProperty]
	public new readonly int DepotValue = 8;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_Grass_Lv2");
}

public class Default : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.None;
	[JsonProperty]
	public new readonly int TeamResonanceId = 10801;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_FIRE_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_FIRE_ENERGY;
	[JsonProperty]
	public new readonly ulong ConfigHash = Utils.AbilityHash("TeamResonance_AllDifferent");
}

public class Frozen : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.Frozen;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_ICE_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_ICE_ENERGY;
}

public class AntiFire : ElementType
{
	[JsonProperty]
	public new readonly Resource.ElementType Type = Resource.ElementType.AntiFire;
	[JsonProperty]
	public new readonly FightProp CurEnergyProp = FightProp.FIGHT_PROP_CUR_FIRE_ENERGY;
	[JsonProperty]
	public new readonly FightProp MaxEnergyProp = FightProp.FIGHT_PROP_MAX_FIRE_ENERGY;
}