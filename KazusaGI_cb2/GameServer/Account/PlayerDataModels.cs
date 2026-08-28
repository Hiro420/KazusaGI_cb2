using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;
using System.Numerics;

namespace KazusaGI_cb2.GameServer.Account;

public class PlayerDataRecord
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string? Id { get; set; }

	[BsonElement("player_uid")]
	public uint PlayerUid { get; set; }

	[BsonElement("scene_id")]
	public uint SceneId { get; set; }

	[BsonElement("pos_x")]
	public float PosX { get; set; }

	[BsonElement("pos_y")]
	public float PosY { get; set; }

	[BsonElement("pos_z")]
	public float PosZ { get; set; }

	[BsonElement("team_index")]
	public uint TeamIndex { get; set; }

	[BsonElement("teams")]
	public List<PlayerTeamSnapshot> Teams { get; set; } = new();

	[BsonElement("items")]
	public List<PlayerItemSnapshot> Items { get; set; } = new();

	[BsonElement("avatars")]
	public List<PlayerAvatarSnapshot> Avatars { get; set; } = new();

	[BsonElement("weapons")]
	public List<PlayerWeaponSnapshot> Weapons { get; set; } = new();

	[BsonElement("level")]
	public int Level { get; set; }

	[BsonElement("opened_gadgets")]
	public List<OpenedGadgetSnapshot> OpenedGadgets { get; set; } = new();

	public Vector3 ToPosition() => new(PosX, PosY, PosZ);
}

public class OpenedGadgetSnapshot
{
	[BsonElement("scene_id")]
	public uint SceneId { get; set; }

	[BsonElement("group_id")]
	public uint GroupId { get; set; }

	[BsonElement("config_id")]
	public uint ConfigId { get; set; }
}

public class PlayerTeamSnapshot
{
	[BsonElement("index")]
	public uint Index { get; set; }

	// GUID-based team state is authoritative. The old AvatarId fields remain
	// for migration of saves created by earlier KazusaGI_cb2 builds.
	[BsonElement("leader_avatar_guid")]
	public ulong LeaderAvatarGuid { get; set; }

	[BsonElement("avatar_guids")]
	public List<ulong> AvatarGuids { get; set; } = new();

	[BsonElement("leader_avatar_id")]
	public uint LeaderAvatarId { get; set; }

	[BsonElement("avatar_ids")]
	public List<uint> AvatarIds { get; set; } = new();
}

public class PlayerItemSnapshot
{
	[BsonElement("guid")]
	public ulong Guid { get; set; }

	[BsonElement("item_id")]
	public uint ItemId { get; set; }

	[BsonElement("count")]
	public uint Count { get; set; }
}

public class PlayerAvatarSnapshot
{
	[BsonElement("guid")]
	public ulong Guid { get; set; }

	[BsonElement("avatar_id")]
	public uint AvatarId { get; set; }

	[BsonElement("level")]
	public uint Level { get; set; }

	[BsonElement("exp")]
	public uint Exp { get; set; }

	[BsonElement("hp")]
	public float Hp { get; set; }

	[BsonElement("max_hp")]
	public float MaxHp { get; set; }

	[BsonElement("def")]
	public float Def { get; set; }

	[BsonElement("atk")]
	public float Atk { get; set; }

	[BsonElement("crit_rate")]
	public float CritRate { get; set; }

	[BsonElement("crit_dmg")]
	public float CritDmg { get; set; }

	[BsonElement("em")]
	public float EM { get; set; }

	[BsonElement("promote_level")]
	public uint PromoteLevel { get; set; }

	[BsonElement("break_level")]
	public uint BreakLevel { get; set; }

	[BsonElement("cur_elem_energy")]
	public float CurElemEnergy { get; set; }

	[BsonElement("skill_depot_id")]
	public uint SkillDepotId { get; set; }

	[BsonElement("ult_skill_id")]
	public uint UltSkillId { get; set; }

	[BsonElement("equip_guid")]
	public ulong EquipGuid { get; set; }

	[BsonElement("skill_levels")]
	[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
	public Dictionary<uint, uint> SkillLevels { get; set; } = new();

	[BsonElement("unlocked_talents")]
	public List<uint> UnlockedTalents { get; set; } = new();

	[BsonElement("proud_skills")]
	public List<uint> ProudSkills { get; set; } = new();

	[BsonElement("buff_ids")]
	public List<uint> BuffIds { get; set; } = new();

	[BsonElement("skill_states")]
	public List<PlayerAvatarSkillSnapshot> SkillStates { get; set; } = new();

	[BsonElement("proud_skill_extra_levels")]
	[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
	public Dictionary<uint, uint> ProudSkillExtraLevels { get; set; } = new();

	[BsonElement("fetter_exp_number")]
	public uint FetterExpNumber { get; set; }

	[BsonElement("fetter_level")]
	public uint FetterLevel { get; set; } = 1;

	[BsonElement("fetter_open_ids")]
	public List<uint> FetterOpenIds { get; set; } = new();
}

public class PlayerAvatarSkillSnapshot
{
	[BsonElement("skill_id")]
	public uint SkillId { get; set; }

	[BsonElement("pass_cd_time")]
	public uint PassCdTime { get; set; }

	[BsonElement("full_cd_times")]
	public List<uint> FullCdTimes { get; set; } = new();

	[BsonElement("max_charge_count")]
	public uint? MaxChargeCount { get; set; }
}

public class PlayerWeaponSnapshot
{
	[BsonElement("guid")]
	public ulong Guid { get; set; }

	[BsonElement("weapon_id")]
	public uint WeaponId { get; set; }

	[BsonElement("level")]
	public uint Level { get; set; }

	[BsonElement("exp")]
	public uint Exp { get; set; }

	[BsonElement("promote_level")]
	public uint PromoteLevel { get; set; }

	[BsonElement("gadget_id")]
	public uint GadgetId { get; set; }

	[BsonElement("equip_guid")]
	public ulong? EquipGuid { get; set; }

	[BsonElement("affix_map")]
	[BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
	public Dictionary<uint, uint> AffixMap { get; set; } = new();
}
