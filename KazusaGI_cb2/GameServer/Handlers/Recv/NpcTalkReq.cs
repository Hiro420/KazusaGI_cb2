using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleNpcTalkReq
{
    [Packet.PacketCmdId(PacketId.NpcTalkReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        NpcTalkReq req = packet.GetDecodedBody<NpcTalkReq>();
        NpcTalkRsp rsp = new NpcTalkRsp()
        {
            NpcEntityId = req.NpcEntityId,
            TalkType = req.TalkType,
            CurTalkId = req.TalkId
        };
        session.SendPacket(rsp);
    }
}