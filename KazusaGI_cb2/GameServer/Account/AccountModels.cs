using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KazusaGI_cb2.GameServer.Account;

public class AccountRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("account_uid")]
    public string AccountUid { get; set; } = string.Empty;

    [BsonElement("account_token")]
    public string AccountToken { get; set; } = string.Empty;

    [BsonElement("player_uid")]
    public uint PlayerUid { get; set; }

    [BsonElement("created_at")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("last_login_at")]
    public DateTime LastLoginAt { get; set; }
}

public class CounterRecord
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("value")]
    public long Value { get; set; }
}

public interface IAccountRepository
{
    AccountRecord? GetByAccountUid(string accountUid);

    AccountRecord GetOrCreate(string accountUid, string accountToken);
}
