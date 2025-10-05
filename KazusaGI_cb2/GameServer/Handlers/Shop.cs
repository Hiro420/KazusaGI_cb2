using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers;

public class Shop
{

    public static Protocol.Shop GetShopByShopType(uint _ShopType)
    {
        ShopType asEnum = (ShopType)_ShopType;
        List<ShopPlanExcelConfig> shopPlanExcelConfigs = MainApp.resourceManager.ShopPlanExcel.Values.Where(c => c.shopType == asEnum).ToList();
        Protocol.Shop shop = new Protocol.Shop()
        {
            ShopType = _ShopType,
        };
        foreach (ShopPlanExcelConfig shopPlan in shopPlanExcelConfigs)
        {
            shop.GoodsLists.Add(ShopPlanExcelToShopGoods(shopPlan));
        }
        return shop;
    }

    public static ShopGoods ShopPlanExcelToShopGoods(ShopPlanExcelConfig shopPlanExcelConfig)
    {
        ShopGoodsExcelConfig shopGood = MainApp.resourceManager.ShopGoodsExcel[shopPlanExcelConfig.goodsId];
        ShopGoods shopGoods = new ShopGoods()
        {
            GoodsId = shopGood.goodsId,
            GoodsItem = new ItemParam()
            {
                ItemId = shopGood.itemId,
                Count = shopGood.itemCount,
            },
            Scoin = shopGood.costScoin,
            Hcoin = shopGood.costHcoin,
            // cost_item_list
            BoughtNum = 0,
            BuyLimit = shopGood.buyLimit,
            BeginTime = 1,
            EndTime = 1999999999,
            NextRefreshTime = 0,
            MinLevel = shopPlanExcelConfig.minPlayerLevel,
            MaxLevel = shopPlanExcelConfig.maxPlayerLevel,
        };
        foreach (IdCountConfig costItem in shopGood.costItems)
        {
            if (costItem.id == 0)
                continue;
            shopGoods.CostItemLists.Add(new ItemParam()
            {
                ItemId = costItem.id,
                Count = costItem.count,
            });
        }
        return shopGoods;
    }
}