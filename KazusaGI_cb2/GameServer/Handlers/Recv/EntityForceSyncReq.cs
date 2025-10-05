using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEntityForceSyncReq
{
    [Packet.PacketCmdId(PacketId.EntityForceSyncReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        EntityForceSyncReq req = packet.GetDecodedBody<EntityForceSyncReq>();
        EntityForceSyncRsp rsp = new EntityForceSyncRsp()
        {
            EntityId = req.EntityId,
            SceneTime = req.SceneTime
        };
        session.SendPacket(rsp);
    }
}