using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetGachaInfoReq
{
    private static uint gachaRandom = 1;

    [Packet.PacketCmdId(PacketId.GetGachaInfoReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        GetGachaInfoRsp rsp = new GetGachaInfoRsp()
        {
            GachaRandom = gachaRandom,
        };
        foreach (GachaExcel excel in MainApp.resourceManager.GachaExcel.Values)
        {
            GachaInfo gachaInfo = new GachaInfo()
            {
                GachaType = excel.costItemId == 224 ? (uint)200 : 300,
                ScheduleId = excel.sortId, // easiest way to diff between them
                BeginTime = 0,
                EndTime = 1999999999,
                CostItemId = excel.costItemId,
                CostItemNum = excel.costItemNum,
                GachaPrefabPath = excel.gachaPrefabPath,
                GachaProbUrl = excel.gachaProbUrl,
                GachaRecordUrl = excel.gachaRecordUrl,
                GachaPreviewPrefabPath = excel.gachaPreviewPrefabPath,
                TenCostItemId = excel.tenCostItemId,
                TenCostItemNum = excel.tenCostItemNum,
                LeftGachaTimes = excel.gachaTimesLimit,
                GachaTimesLimit = excel.gachaTimesLimit,
                GachaSortId = excel.sortId,
            };
            rsp.GachaInfoLists.Add(gachaInfo);
        }
        session.SendPacket(rsp);
    }
}