using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSceneEntityDrownReq
{
    [Packet.PacketCmdId(PacketId.SceneEntityDrownReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        SceneEntityDrownReq req = packet.GetDecodedBody<SceneEntityDrownReq>();
        SceneEntityDrownRsp rsp = new SceneEntityDrownRsp()
        {
            EntityId = req.EntityId
        };

        if (!session.entityMap.ContainsKey(req.EntityId)) // todo: prevent it from happening
        {
            session.SendPacket(rsp);
            return;
        }
        object entity = session.entityMap[req.EntityId];
        // todo: implement drowning for player
        if (entity is MonsterEntity)
        {
            MonsterEntity monster = (MonsterEntity)entity;
            monster.Die(VisionType.VisionMiss);
        }
        session.SendPacket(rsp);
    }
}