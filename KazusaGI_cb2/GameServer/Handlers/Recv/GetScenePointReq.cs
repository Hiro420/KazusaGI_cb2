using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetScenePointReq
{
    [Packet.PacketCmdId(PacketId.GetScenePointReq)]
    public static void OnPacket(Session session, Packet packet)
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
}