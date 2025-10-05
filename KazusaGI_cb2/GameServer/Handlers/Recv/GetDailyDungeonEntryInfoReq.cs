using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetDailyDungeonEntryInfoReq
{
    // maybe make it actually work with the calendar? idk
    [Packet.PacketCmdId(PacketId.GetDailyDungeonEntryInfoReq)]
    public static void OnPacket(Session session, Packet packet)
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
        }
        ;
        session.SendPacket(rsp);
    }
}
