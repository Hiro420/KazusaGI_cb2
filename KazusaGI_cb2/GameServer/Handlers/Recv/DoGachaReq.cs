using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleDoGachaReq
{
    private static uint gachaRandom = 1;

    [Packet.PacketCmdId(PacketId.DoGachaReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        Random random = new Random();
        DoGachaReq req = packet.GetDecodedBody<DoGachaReq>();
        DoGachaRsp rsp = new DoGachaRsp();
        uint gachaTimes = req.GachaTimes;
        GachaExcel gachaExcel = MainApp.resourceManager.GachaExcel.Values.First(c => c.sortId == req.GachaScheduleId);
        List<GachaPoolExcel> gachaPoolExcels = MainApp.resourceManager.GachaPoolExcel[gachaExcel.poolId];
        for (int i = 0; i < gachaTimes; i++)
        {
            List<GachaPoolExcel> filteredPool = gachaPoolExcels
                .Where(entry =>
                    entry.poolRootId == gachaExcel.poolId
                    && (entry.itemType == GachaItemType.GACHA_ITEM_AVATAR_S5 || entry.itemType == GachaItemType.GACHA_ITEM_WEAPON_S5)) // its a ps, let people have fun
                .ToList();

            // Get a random item from the filtered pool
            Console.WriteLine($"Filtered pool count: {filteredPool.Count}");
            Console.WriteLine($"{JsonConvert.SerializeObject(filteredPool)}");
            GachaPoolExcel randomEntry = filteredPool[random.Next(filteredPool.Count)];
            uint randItemId = randomEntry.itemId;
            rsp.GachaItemLists.Add(new GachaItem()
            {
                gacha_item = new ItemParam()
                {
                    ItemId = randItemId,
                    Count = 1,
                },
                IsGachaItemNew = true,
            });
        }
        gachaRandom += 1;
        session.SendPacket(rsp);
    }
}