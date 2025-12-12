using System;
using KazusaGI_cb2.GameServer.Account;

namespace KazusaGI_cb2.GameServer.Account;

public static class AccountManager
{
    private static readonly Logger Logger = new("AccountManager");
    private static readonly IAccountRepository Repository;
    private static readonly MongoAccountRepository MongoRepository;

    static AccountManager()
    {
        try
        {
            var cfg = MainApp.config.AccountDataBase;
            MongoRepository = new MongoAccountRepository(cfg);
            Repository = MongoRepository;
            Logger.LogInfo($"Initialized Mongo account repository at {cfg.Uri}, db '{cfg.Collection}'");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to initialize account repository: {ex.Message}\n{ex}");
            throw;
        }
    }

    public static AccountRecord GetOrCreate(string accountUid, string accountToken)
        => Repository.GetOrCreate(accountUid, accountToken);

    public static AccountRecord? GetByAccountUid(string accountUid)
        => Repository.GetByAccountUid(accountUid);

    // Expose strongly-typed player data helpers for now via the concrete Mongo implementation. 
    public static PlayerDataRecord? LoadPlayerData(uint playerUid)
    {
        return MongoRepository.LoadPlayerData(playerUid);
    }

    public static void SavePlayerData(PlayerDataRecord record)
    {
        MongoRepository.SavePlayerData(record);
    }
}
