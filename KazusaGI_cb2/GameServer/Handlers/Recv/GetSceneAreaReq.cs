using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetSceneAreaReq
{
    [Packet.PacketCmdId(PacketId.GetSceneAreaReq)]
    public static void OnPacket(Session session, Packet packet)
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
}