using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Scene;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers;

public class Dungeon
{
    [Packet.PacketCmdId(PacketId.DungeonEntryInfoReq)]
    public static void HandleDungeonEntryInfoReq(Session session, Packet packet)
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

    // maybe make it actually work with the calendar? idk
    [Packet.PacketCmdId(PacketId.GetDailyDungeonEntryInfoReq)]
    public static void HandleGetDailyDungeonEntryInfoReq(Session session, Packet packet)
    {
        GetDailyDungeonEntryInfoReq req = packet.GetDecodedBody<GetDailyDungeonEntryInfoReq>();
        GetDailyDungeonEntryInfoRsp rsp = new GetDailyDungeonEntryInfoRsp();
        ScenePoint scenePoint = MainApp.resourceManager.ScenePoints[session.player!.SceneId];
        foreach (KeyValuePair<uint, ConfigScenePoint> configScenePoint_kvp in scenePoint.points)
        {
            ConfigScenePoint configScenePoint = configScenePoint_kvp.Value;
            if (configScenePoint.dungeonRandomList == null || configScenePoint.dungeonRandomList.Count == 0)
                continue;
            foreach (uint dungeonConfigId in configScenePoint.dungeonRandomList)
            {
                if (!MainApp.resourceManager.DailyDungeonExcel.ContainsKey(dungeonConfigId))
                    continue;
                DailyDungeonEntryInfo dailyDungeonEntryInfo = new DailyDungeonEntryInfo()
                {
                    DungeonEntryId = configScenePoint_kvp.Key,
                    DungeonEntryConfigId = dungeonConfigId,
                    RecommendDungeonId = MainApp.resourceManager.DailyDungeonExcel[dungeonConfigId].monday.First()
                };
                rsp.DailyDungeonInfoLists.Add(dailyDungeonEntryInfo);
            }
        };
        session.SendPacket(rsp);
    }

    [Packet.PacketCmdId(PacketId.PlayerEnterDungeonReq)]
    public static void HandlePlayerEnterDungeonReq(Session session, Packet packet)
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

    [Packet.PacketCmdId(PacketId.PlayerQuitDungeonReq)]
    public static void HandlePlayerQuitDungeonReq(Session session, Packet packet)
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