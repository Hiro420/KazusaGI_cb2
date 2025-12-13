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
        var req = packet.GetDecodedBody<EntityForceSyncReq>();

        var rsp = new EntityForceSyncRsp
        {
            EntityId = req.EntityId,
            SceneTime = req.SceneTime,
            Retcode = 0
        };

        var player = session.player;
        if (player?.Scene == null || !player.Scene.EntityManager.TryGet(req.EntityId, out var entity))
        {
            rsp.Retcode = -1;
            session.SendPacket(rsp);
            return;
        }

        var motion = req.MotionInfo;
        if (motion != null)
        {
            entity.Position = Session.VectorProto2Vector3(motion.Pos);
            entity.Rotation = Session.VectorProto2Vector3(motion.Rot);
        }

        session.SendPacket(rsp);
    }
}