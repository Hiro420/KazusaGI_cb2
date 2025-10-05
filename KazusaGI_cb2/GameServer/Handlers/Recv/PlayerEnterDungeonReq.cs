using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePlayerEnterDungeonReq
{
    [Packet.PacketCmdId(PacketId.PlayerEnterDungeonReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        PlayerEnterDungeonReq req = packet.GetDecodedBody<PlayerEnterDungeonReq>();
        PlayerEnterDungeonRsp rsp = new PlayerEnterDungeonRsp()
        {
            DungeonId = req.DungeonId,
            PointId = req.PointId
        };
        session.player!.Overworld_PointId = req.PointId; // backup
        DungeonExcelConfig dungeonExcelConfig = MainApp.resourceManager.DungeonExcel[req.DungeonId];
        ConfigScenePoint configScenePoint = MainApp.resourceManager.ScenePoints[session.player.SceneId].points[req.PointId];
        SceneLua sceneLua = MainApp.resourceManager.SceneLuas[dungeonExcelConfig.sceneId];
        Vector3 transPos = sceneLua.scene_config.born_pos;
        Vector3 transRot = sceneLua.scene_config.born_rot;
        session.player.TeleportToPos(session, transPos, true);
        session.player.SetRot(transRot);
        session.player.EnterScene(session, dungeonExcelConfig.sceneId, EnterType.EnterDungeon);
        session.SendPacket(rsp);
    }
}
