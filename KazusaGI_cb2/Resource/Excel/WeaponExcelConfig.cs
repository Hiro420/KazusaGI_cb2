namespace KazusaGI_cb2.Resource.Excel;

public class WeaponExcelConfig
{
	public WeaponType weaponType; // 0x48
	public uint rankLevel; // 0x4C
	public bool isGold; // 0x58
	public uint weaponBaseExp; // 0x5C
	public List<uint> skillAffix; // 0x60
								  // public List<WeaponProperty> weaponProp; // 0x68
	public string awakenTexture; // 0x70
	public string awakenLightMapTexture; // 0x78
	public bool unRotate; // 0x80
	public uint weaponPromoteId; // 0x84
	public uint storyId; // 0x88
	public List<uint> awakenCosts; // 0x90
	public byte gachaCardNameHashPre; // 0x98
	public uint gachaCardNameHashSuffix; // 0x9C

	public List<WeaponProperty> weaponProp;

	public uint id; // 0x10
	public uint nameTextMapHash; // 0x14
	public uint descTextMapHash; // 0x18
	public string icon; // 0x20
						// public ItemType itemType; // 0x28
	public uint weight; // 0x2C
	public uint rank; // 0x30
	public uint gadgetId; // 0x34
	public bool dropable; // 0x38
	public uint useLevel; // 0x3C
	public uint globalItemLimit; // 0x40
}