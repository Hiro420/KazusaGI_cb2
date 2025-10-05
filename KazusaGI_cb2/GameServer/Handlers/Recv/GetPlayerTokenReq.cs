using KazusaGI_cb2.GameServer.PlayerInfos;
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
        GetPlayerTokenRsp rsp = new GetPlayerTokenRsp()
        {
            Uid = 69,
            AccountType = req.AccountType,
            AccountUid = req.AccountUid,
            Token = req.AccountToken,
            GmUid = Convert.ToUInt32(req.AccountUid),
            SecretKeySeed = Convert.ToUInt64(req.AccountUid)
        };
        session.player = new Player(session, 69);
        session.player.InitTeams();
        session.player.AddAllAvatars(session);
        session.player.AddAllMaterials(session, true);
        session.SendPacket(rsp);
        session.key = NewKey(Convert.ToUInt64(req.AccountUid));
    }
}