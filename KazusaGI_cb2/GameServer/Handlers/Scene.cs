using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using static KazusaGI_cb2.Utils.Crypto;

namespace KazusaGI_cb2.GameServer.Handlers;

public class Scene
{
    private static SemaphoreSlim _updateOnMoveSemaphore = new SemaphoreSlim(1, 1);

    [Packet.PacketCmdId(PacketId.SceneEntitiesMovesReq)]
    public static void HandleSceneEntitiesMovesReq(Session session, Packet packet)
    {
        SceneEntitiesMovesReq req = packet.GetDecodedBody<SceneEntitiesMovesReq>();
        SceneEntitiesMovesRsp rsp = new SceneEntitiesMovesRsp();
        bool needsUpdate = false;
        foreach (EntityMoveInfo move in req.EntityMoveInfoLists)
        {
            if (Session.VectorProto2Vector3(move.MotionInfo.Pos) == Vector3.Zero || !session.entityMap.ContainsKey(move.EntityId))
            {
                session.c.LogWarning($"[FUCKED MOVEMENT] Entity {move.EntityId} moved to {move.MotionInfo.Pos.X}, {move.MotionInfo.Pos.Y}, {move.MotionInfo.Pos.Z}");
                // may happen sometimes, may not. better be safe.
                continue;
            }
            session.entityMap[move.EntityId].Position = Session.VectorProto2Vector3(move.MotionInfo.Pos);
            if (session.entityMap[move.EntityId] is AvatarEntity)
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

    [Packet.PacketCmdId(PacketId.SceneGetAreaExplorePercentReq)]
    public static void HandleSceneGetAreaExplorePercentReq(Session session, Packet packet)
    {
        SceneGetAreaExplorePercentReq req = packet.GetDecodedBody<SceneGetAreaExplorePercentReq>();
        SceneGetAreaExplorePercentRsp rsp = new SceneGetAreaExplorePercentRsp()
        {
            AreaId = req.AreaId,
            ExplorePercent = 100
        };
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.SceneTransToPointReq)]
    public static void HandleSceneTransToPointReq(Session session, Packet packet)
    {
        SceneTransToPointReq req = packet.GetDecodedBody<SceneTransToPointReq>();

        ConfigScenePoint scenePoint = MainApp.resourceManager.ScenePoints[req.SceneId].points[req.PointId];

        session.player!.SetRot(scenePoint.tranRot);
        session.player.TeleportToPos(session, scenePoint.tranPos, true);
        session.player.EnterScene(session, req.SceneId, EnterType.EnterGoto);

        SceneTransToPointRsp rsp = new SceneTransToPointRsp()
        {
            PointId = req.PointId,
            SceneId = req.SceneId
        };
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.GetScenePointReq)]
    public static void HandleGetScenePointReq(Session session, Packet packet)
    {
        GetScenePointReq req = packet.GetDecodedBody<GetScenePointReq>();
        GetScenePointRsp rsp = new GetScenePointRsp();
        foreach (KeyValuePair<uint, ConfigScenePoint> kvp in MainApp.resourceManager.ScenePoints[req.SceneId].points)
        {
            rsp.UnlockedPointLists.Add(kvp.Key);
            rsp.SceneId = req.SceneId;
            rsp.BelongUid = req.BelongUid;

            if (!rsp.UnlockAreaLists.Contains(kvp.Value.areaId) && kvp.Value.areaId != 0)
                rsp.UnlockAreaLists.Add(kvp.Value.areaId);
        }
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.EnterWorldAreaReq)]
    public static void HandleEnterWorldAreaReq(Session session, Packet packet)
    {
        EnterWorldAreaReq req = packet.GetDecodedBody<EnterWorldAreaReq>();
        EnterWorldAreaRsp rsp = new EnterWorldAreaRsp()
        {
            AreaId = req.AreaId,
            AreaType = req.AreaType
        };
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.GetSceneAreaReq)]
    public static void HandleGetSceneAreaReq(Session session, Packet packet)
    {
        GetSceneAreaReq req = packet.GetDecodedBody<GetSceneAreaReq>();
        GetSceneAreaRsp rsp = new GetSceneAreaRsp()
        {
            SceneId = req.SceneId,
        };
        foreach (ConfigScenePoint scenePoint in MainApp.resourceManager.ScenePoints[session.player!.SceneId].points.Values)
        {
            if (!rsp.AreaIdLists.Contains(scenePoint.areaId) && scenePoint.areaId != 0)
                rsp.AreaIdLists.Add(scenePoint.areaId);
        }
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.SceneEntityDrownReq)]
    public static void HandleSceneEntityDrownReq(Session session, Packet packet)
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
