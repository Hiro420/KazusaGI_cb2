using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleDungeonEntryInfoReq
{
    [Packet.PacketCmdId(PacketId.DungeonEntryInfoReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        DungeonEntryInfoReq req = packet.GetDecodedBody<DungeonEntryInfoReq>();
        ConfigScenePoint scenePoint = MainApp.resourceManager.ScenePoints[session.player!.SceneId].points[req.PointId];
        List<uint> ids = new List<uint>();
        if (scenePoint.dungeonIds.Count > 0)
            ids.AddRange(scenePoint.dungeonIds);
        else
        {
            foreach (uint id in scenePoint.dungeonRandomList)
            {
                DailyDungeonConfig dailyDungeonConfig = MainApp.resourceManager.DailyDungeonExcel[id];
                ids.AddRange(dailyDungeonConfig.monday);
            }
        }
        DungeonEntryInfoRsp rsp = new DungeonEntryInfoRsp()
        {
            PointId = req.PointId,
        };
        DungeonExcelConfig recommmendDungeon = MainApp.resourceManager.DungeonExcel[ids.First()];
        foreach (uint dungeonId in ids)
        {
            if (!MainApp.resourceManager.DungeonExcel.ContainsKey(dungeonId))
                continue;
            DungeonExcelConfig dungeonExcel = MainApp.resourceManager.DungeonExcel[dungeonId];
            DungeonEntryInfo entryInfo = new DungeonEntryInfo()
            {
                DungeonId = dungeonId,
                BossChestNum = 0,
                EndTime = 1999999999,
                IsPassed = false,
                LeftTimes = 3,
                MaxBossChestNum = 3, // idk where they are in game data
                StartTime = 0,
            };
            if (dungeonExcel.levelRevise > recommmendDungeon.levelRevise && dungeonExcel.levelRevise <= session.player.Level)
            {
                recommmendDungeon = dungeonExcel;
            }
            rsp.DungeonEntryLists.Add(entryInfo);
        }
        rsp.RecommendDungeonId = recommmendDungeon.id;
        session.SendPacket(rsp);
    }
}
