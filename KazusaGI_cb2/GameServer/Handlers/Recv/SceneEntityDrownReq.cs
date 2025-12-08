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
        EntityManager entityManager = session.player!.Scene.EntityManager;
        SceneEntityDrownReq req = packet.GetDecodedBody<SceneEntityDrownReq>();
        SceneEntityDrownRsp rsp = new SceneEntityDrownRsp()
        {
            EntityId = req.EntityId
        };

        if (!entityManager.TryGet(req.EntityId, out Entity? entity)) // todo: prevent it from happening
        {
            session.SendPacket(rsp);
            return;
        }
        // todo: implement drowning for player
        if (entity is MonsterEntity)
        {
            MonsterEntity monster = (MonsterEntity)entity;
            monster.Die(VisionType.VisionMiss);
        }
        session.SendPacket(rsp);
    }
}