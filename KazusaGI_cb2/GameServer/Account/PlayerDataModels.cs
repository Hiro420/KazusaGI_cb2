using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
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

    public Vector3 ToPosition() => new(PosX, PosY, PosZ);
}

public class PlayerTeamSnapshot
{
    [BsonElement("index")]
    public uint Index { get; set; }

    [BsonElement("leader_avatar_id")]
    public uint LeaderAvatarId { get; set; }

    [BsonElement("avatar_ids")]
    public List<uint> AvatarIds { get; set; } = new();
}

public class PlayerItemSnapshot
{
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
    public Dictionary<uint, uint> SkillLevels { get; set; } = new();

    [BsonElement("unlocked_talents")]
    public List<uint> UnlockedTalents { get; set; } = new();

    [BsonElement("proud_skills")]
    public List<uint> ProudSkills { get; set; } = new();
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
}
