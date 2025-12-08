using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSceneEntitiesMovesReq
{
    [Packet.PacketCmdId(PacketId.SceneEntitiesMovesReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        SceneEntitiesMovesReq req = packet.GetDecodedBody<SceneEntitiesMovesReq>();
        SceneEntitiesMovesRsp rsp = new SceneEntitiesMovesRsp();
        bool needsUpdate = false;
        EntityManager entityManager = session.player!.Scene.EntityManager;
        foreach (EntityMoveInfo move in req.EntityMoveInfoLists)
        {
            if (Session.VectorProto2Vector3(move.MotionInfo.Pos) == Vector3.Zero || !entityManager.TryGet(move.EntityId, out Entity? entity))
            {
                session.c.LogWarning($"[FUCKED MOVEMENT] Entity {move.EntityId} moved to {move.MotionInfo.Pos.X}, {move.MotionInfo.Pos.Y}, {move.MotionInfo.Pos.Z}");
                // may happen sometimes, may not. better be safe.
                continue;
            }
            entity.Position = Session.VectorProto2Vector3(move.MotionInfo.Pos);
            if (entity is AvatarEntity)
            {
                needsUpdate = true;
                session.player!.TeleportToPos(session, Session.VectorProto2Vector3(move.MotionInfo.Pos), true);
                session.player!.SetRot(Session.VectorProto2Vector3(move.MotionInfo.Rot));
                // session.c.LogWarning($"Player {session.player.Uid} moved to {move.MotionInfo.Pos.X}, {move.MotionInfo.Pos.Y}, {move.MotionInfo.Pos.Z}");
            }
        }
        session.SendPacket(rsp);

        if (needsUpdate)
            session.player!.Scene.UpdateOnMove();
    }
}