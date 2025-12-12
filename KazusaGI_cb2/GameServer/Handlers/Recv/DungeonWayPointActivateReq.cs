using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleDungeonWayPointActivateReq
{
    [Packet.PacketCmdId(PacketId.DungeonWayPointActivateReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        var req = packet.GetDecodedBody<DungeonWayPointActivateReq>();
        var rsp = new DungeonWayPointActivateRsp
        {
            WayPointId = req.WayPointId,
            Retcode = (int)Retcode.RetSucc
        };

        var player = session.player;
        if (player == null)
        {
            rsp.Retcode = (int)Retcode.RetFail;
            session.SendPacket(rsp);
            return;
        }

        // In hk4e, this handler only works inside a dungeon scene (DungeonScene).
        // We approximate this by requiring the current scene id to come from a dungeon
        // entry; if there is no ScenePoints entry at all, we fail early.
        if (!MainApp.resourceManager.ScenePoints.TryGetValue(player.SceneId, out ScenePoint? scenePoint) ||
            scenePoint.points == null ||
            !scenePoint.points.TryGetValue(req.WayPointId, out ConfigScenePoint? configPoint))
        {
            session.c.LogWarning($"[Dungeon] DungeonWayPointActivateReq: cannot find scene point, scene_id={player.SceneId}, point_id={req.WayPointId}");
            rsp.Retcode = (int)Retcode.RetFail;
            session.SendPacket(rsp);
            return;
        }

        // hk4e checks the current avatar; if it's missing, they return RET_CAN_NOT_FIND_AVATAR (104).
        var currentLineup = player.GetCurrentLineup();
        if (currentLineup.Leader == null)
        {
            rsp.Retcode = (int)Retcode.RetCanNotFindAvatar;
            session.SendPacket(rsp);
            return;
        }

        // Distance check: hk4e uses a server config value and getDistance.
        // Here we use a reasonable fixed radius around the waypoint center.
        const float maxDistance = 5.0f;
        var avatarPos = player.Pos;
        var wpPos = configPoint.pos;

        float dx = avatarPos.X - wpPos.X;
        float dy = avatarPos.Y - wpPos.Y;
        float dz = avatarPos.Z - wpPos.Z;
        float distSq = dx * dx + dy * dy + dz * dz;

        if (distSq > maxDistance * maxDistance)
        {
            rsp.Retcode = (int)Retcode.RetDistanceLong;
            session.SendPacket(rsp);
            return;
        }

        // Mark this waypoint as active for the current dungeon context.
        player.ActiveDungeonWayPoints.Add(req.WayPointId);

        // Mirror hk4e's network behavior:
        // - set rsp.way_point_id
        // - push DungeonWayPointNotify with is_add = true and the activated id.
        var notify = new DungeonWayPointNotify
        {
            IsAdd = true
        };
        notify.ActiveWayPointLists.Add(req.WayPointId);
        session.SendPacket(notify);

        session.SendPacket(rsp);
    }
}
