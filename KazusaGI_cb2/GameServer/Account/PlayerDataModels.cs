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
