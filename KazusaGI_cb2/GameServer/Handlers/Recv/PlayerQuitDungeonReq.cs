using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePlayerQuitDungeonReq
{
    [Packet.PacketCmdId(PacketId.PlayerQuitDungeonReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        uint destPointId;
        PlayerQuitDungeonReq req = packet.GetDecodedBody<PlayerQuitDungeonReq>();
        PlayerQuitDungeonRsp rsp = new PlayerQuitDungeonRsp();
        if (session.player!.towerInstance != null)
        {
            destPointId = session.player!.towerInstance._towerPointId;
            session.player!.towerInstance.EndInstance();
        }
        else
        {
            destPointId = req.PointId != 0 ? req.PointId : session.player!.Overworld_PointId;
        }
        ConfigScenePoint configScenePoint = MainApp.resourceManager.ScenePoints[3].points[destPointId];
        session.player!.TeleportToPos(session, configScenePoint.tranPos, true);
        session.player!.SetRot(configScenePoint.tranRot);
        session.player.EnterScene(session, 3, EnterType.EnterSelf);
        session.SendPacket(rsp);
    }
}
