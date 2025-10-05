using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleTowerAllDataReq
{
    [Packet.PacketCmdId(PacketId.TowerAllDataReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        uint curScheduleId = MainApp.resourceManager.TowerScheduleExcel.Keys.Last();
        TowerAllDataRsp rsp = new TowerAllDataRsp()
        {
            TowerScheduleId = curScheduleId,
            NextScheduleChangeTime = 1999999999,
            CurLevelRecord = new TowerCurLevelRecord()
            {
                IsEmpty = true,
            },
        };
        List<uint> floorList = MainApp.resourceManager.TowerScheduleExcel[curScheduleId].schedules.FindAll(c => c.floorList.Count > 0).Last().floorList;
        foreach (uint floor in floorList)
        {
            rsp.FloorOpenTimeMaps.Add(floor, 1);
            uint levelGroupId = MainApp.resourceManager.TowerFloorExcel[floor].levelGroupId;
            List<TowerLevelExcelConfig> towerLevelExcelConfigs = MainApp.resourceManager.TowerLevelExcel.Values.ToList().FindAll(c => c.levelGroupId == levelGroupId);
            TowerFloorRecord towerFloorRecord = new TowerFloorRecord()
            {
                FloorId = floor,
                FloorStarRewardProgress = (uint)(3 * towerLevelExcelConfigs.Count)
            };
            for (int i = 0; i < towerLevelExcelConfigs.Count; i++)
            {
                TowerLevelExcelConfig towerLevelExcelConfig = towerLevelExcelConfigs[i];
                towerFloorRecord.PassedLevelMaps.Add(towerLevelExcelConfig.levelId, 3);
                TowerLevelRecord towerLevelRecord = new TowerLevelRecord()
                {
                    LevelId = towerLevelExcelConfig.levelId,
                };
                foreach (uint cond in Enumerable.Range(1, towerLevelExcelConfig.conds.Count).Select(x => (uint)x))
                {
                    towerLevelRecord.SatisfiedCondLists.Add(cond);
                }
                towerFloorRecord.PassedLevelRecordLists.Add(towerLevelRecord);
                rsp.TowerFloorRecordLists.Add(towerFloorRecord);
            }
        }
        session.SendPacket(rsp);
    }
}