using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Json.Scene;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleExitTransPointRegionNotify
{
    [Packet.PacketCmdId(PacketId.ExitTransPointRegionNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var notify = packet.GetDecodedBody<ExitTransPointRegionNotify>();
        // Mirror hk4e's behavior: only tower SceneTransPoints are relevant for first trans point region tracking.

        if (!MainApp.resourceManager.ScenePoints.TryGetValue(notify.SceneId, out var scenePoints) ||
            scenePoints.points == null ||
            !scenePoints.points.TryGetValue(notify.PointId, out ConfigScenePoint? configPoint))
        {
            session.c.LogWarning($"ExitTransPointRegionNotify: sceneId={notify.SceneId} pointId={notify.PointId} not found in ScenePoints");
            return;
        }

        if (configPoint.Type == ScenePointType.TOWER)
        {
            session.player!.OnExitFirstTransPointRegion(notify.PointId);
        }
        else
        {
            session.c.LogInfo($"ExitTransPointRegionNotify: pointId={notify.PointId} is not TOWER (type={configPoint.Type})");
        }
    }
}
