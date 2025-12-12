using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.GameServer.Account;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using static KazusaGI_cb2.Utils.Crypto;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetPlayerTokenReq
{
    [Packet.PacketCmdId(PacketId.GetPlayerTokenReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        GetPlayerTokenReq req = packet.GetDecodedBody<GetPlayerTokenReq>();
        // Use persistent account storage to resolve or create a player UID
        var account = AccountManager.GetOrCreate(req.AccountUid, req.AccountToken);

        GetPlayerTokenRsp rsp = new GetPlayerTokenRsp()
        {
            Uid = account.PlayerUid,
            AccountType = req.AccountType,
            AccountUid = req.AccountUid,
            Token = req.AccountToken,
            GmUid = account.PlayerUid,
            SecretKeySeed = Convert.ToUInt64(req.AccountUid)
        };

        session.player = new Player(session, account.PlayerUid);
        session.player.MpLevelEntity = new MpLevelEntity(session);
        session.player.InitTeams();
        session.player.AddBasicAvatar();
        //session.player.AddAllAvatars(session);
        //session.player.AddAllMaterials(session, true);

        // Load persisted basic player state (scene, pos, teams, inventory) if present
        var saved = AccountManager.LoadPlayerData(account.PlayerUid);
        saved ??= new PlayerDataRecord
        {
            PlayerUid = account.PlayerUid,
            SceneId = session.player.SceneId,
            PosX = session.player.Pos.X,
            PosY = session.player.Pos.Y,
            PosZ = session.player.Pos.Z,
            TeamIndex = session.player.TeamIndex,
            Level = session.player.Level
        };

        session.player.ApplyPlayerDataRecord(saved);
        AccountManager.SavePlayerData(session.player.ToPlayerDataRecord());
        session.SendPacket(rsp);
        session.key = NewKey(Convert.ToUInt64(req.AccountUid));
    }
}