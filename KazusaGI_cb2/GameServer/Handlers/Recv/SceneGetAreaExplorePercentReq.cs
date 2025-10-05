using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleSceneGetAreaExplorePercentReq
{
    [Packet.PacketCmdId(PacketId.SceneGetAreaExplorePercentReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        SceneGetAreaExplorePercentReq req = packet.GetDecodedBody<SceneGetAreaExplorePercentReq>();
        SceneGetAreaExplorePercentRsp rsp = new SceneGetAreaExplorePercentRsp()
        {
            AreaId = req.AreaId,
            ExplorePercent = 100
        };
        session.SendPacket(rsp);
    }
}