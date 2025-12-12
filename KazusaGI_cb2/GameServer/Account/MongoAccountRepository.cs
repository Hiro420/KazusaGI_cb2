using System;
using KazusaGI_cb2.GameServer.Account;
using MongoDB.Driver;

namespace KazusaGI_cb2.GameServer.Account;

public class MongoAccountRepository : IAccountRepository
{
    private readonly IMongoCollection<AccountRecord> _accounts;
    private readonly IMongoCollection<CounterRecord> _counters;
    private readonly IMongoCollection<PlayerDataRecord> _playerData;

    public MongoAccountRepository(AccountDataBaseInfo config)
    {
        var client = new MongoClient(config.Uri);

        // Treat the "Collection" config as the database name to keep
        // backward compatibility; within it we keep separate collections.
        var database = client.GetDatabase(config.Collection);

        _accounts = database.GetCollection<AccountRecord>("accounts");
        _counters = database.GetCollection<CounterRecord>("counters");
        _playerData = database.GetCollection<PlayerDataRecord>("player_data");
    }

    public AccountRecord? GetByAccountUid(string accountUid)
    {
        return _accounts.Find(a => a.AccountUid == accountUid).FirstOrDefault();
    }

    public AccountRecord? GetByAccountName(string accountName)
    {
        return _accounts.Find(a => a.Name == accountName).FirstOrDefault();
	}

    public AccountRecord? GetByAccountToken(string accountToken)
    {
        return _accounts.Find(a => a.AccountToken == accountToken).FirstOrDefault();
	}

	public AccountRecord GetOrCreate(string accountName, string accountToken)
    {
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new ArgumentException("accountName must not be empty", nameof(accountName));
        }

        var existing = GetByAccountName(accountName);
        if (existing != null)
        {
            var update = Builders<AccountRecord>.Update
                .Set(a => a.AccountToken, accountToken)
                .Set(a => a.LastLoginAt, DateTime.UtcNow);

            _accounts.UpdateOne(a => a.Id == existing.Id, update);

            existing.AccountToken = accountToken;
            existing.LastLoginAt = DateTime.UtcNow;
            return existing;
        }

        uint playerUid = (uint)AllocateNextPlayerUid();
        string accountUid = AllocateNextAccountUid();

        var now = DateTime.UtcNow;
        var record = new AccountRecord
        {
			AccountUid = accountUid,
            Name = accountName,
			AccountToken = accountToken,
            PlayerUid = playerUid,
            CreatedAt = now,
            LastLoginAt = now
        };

        _accounts.InsertOne(record);
        return record;
    }

    private long AllocateNextPlayerUid()
    {
        var filter = Builders<CounterRecord>.Filter.Eq(c => c.Id, "player_uid");
        var update = Builders<CounterRecord>.Update.Inc(c => c.Value, 1);

        var options = new FindOneAndUpdateOptions<CounterRecord>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var counter = _counters.FindOneAndUpdate(filter, update, options);

        // If this is the first time, initialize starting UID at 1
        if (counter == null)
        {
            var seed = new CounterRecord { Id = "player_uid", Value = 1 };
            _counters.InsertOne(seed);
            return seed.Value;
        }

        // Ensure a reasonable lower bound
        if (counter.Value < 1)
        {
            var correction = Builders<CounterRecord>.Update.Set(c => c.Value, 1);
            counter = _counters.FindOneAndUpdate(filter, correction, new FindOneAndUpdateOptions<CounterRecord>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            });
        }

        return counter.Value;
    }

    private string AllocateNextAccountUid()
    {
        var filter = Builders<CounterRecord>.Filter.Eq(c => c.Id, "account_uid");
        var update = Builders<CounterRecord>.Update.Inc(c => c.Value, 1);

        var options = new FindOneAndUpdateOptions<CounterRecord>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var counter = _counters.FindOneAndUpdate(filter, update, options);

        // If this is the first time, initialize starting UID at 1
        if (counter == null)
        {
            var seed = new CounterRecord { Id = "account_uid", Value = 1 };
            _counters.InsertOne(seed);
            return seed.Value.ToString();
        }

        // Ensure a reasonable lower bound
        if (counter.Value < 1)
        {
            var correction = Builders<CounterRecord>.Update.Set(c => c.Value, 1);
            counter = _counters.FindOneAndUpdate(filter, correction, new FindOneAndUpdateOptions<CounterRecord>
            {
                IsUpsert = true,
                ReturnDocument = ReturnDocument.After
            });
        }

        return counter.Value.ToString();
    }

    #region Player data helpers

    public PlayerDataRecord? LoadPlayerData(uint playerUid)
    {
        return _playerData.Find(p => p.PlayerUid == playerUid).FirstOrDefault();
    }

    public void SavePlayerData(PlayerDataRecord record)
    {
        var filter = Builders<PlayerDataRecord>.Filter.Eq(p => p.PlayerUid, record.PlayerUid);
        var existing = _playerData.Find(filter).FirstOrDefault();

        if (existing == null)
        {
            _playerData.InsertOne(record);
        }
        else
        {
            record.Id = existing.Id;
            _playerData.ReplaceOne(filter, record);
        }
    }

    #endregion
}
