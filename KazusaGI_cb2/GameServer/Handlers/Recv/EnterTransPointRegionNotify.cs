using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Json.Scene;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEnterTransPointRegionNotify
{
    [Packet.PacketCmdId(PacketId.EnterTransPointRegionNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var notify = packet.GetDecodedBody<EnterTransPointRegionNotify>();
        // Mirror hk4e's behavior:
        // - Look up the SceneTransPoint config
        // - If it's a TOWER point, notify the player avatar comp hat we've entered the first trans point region.

        if (!MainApp.resourceManager.ScenePoints.TryGetValue(notify.SceneId, out var scenePoints) ||
            scenePoints.points == null ||
            !scenePoints.points.TryGetValue(notify.PointId, out ConfigScenePoint? configPoint))
        {
            session.c.LogWarning($"EnterTransPointRegionNotify: sceneId={notify.SceneId} pointId={notify.PointId} not found in ScenePoints");
            return;
        }

        if (configPoint.Type == ScenePointType.TOWER)
        {
            session.player!.OnEnterFirstTransPointRegion(notify.PointId);
        }
        else
        {
            session.c.LogInfo($"EnterTransPointRegionNotify: pointId={notify.PointId} is not TOWER (type={configPoint.Type})");
        }
    }
}
