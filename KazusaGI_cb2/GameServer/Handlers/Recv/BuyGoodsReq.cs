using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleBuyGoodsReq
{
    [Packet.PacketCmdId(PacketId.BuyGoodsReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        BuyGoodsReq req = packet.GetDecodedBody<BuyGoodsReq>();
        BuyGoodsRsp rsp = new BuyGoodsRsp()
        {
            ShopType = req.ShopType,
            Goods = req.Goods,
            BuyCount = req.BuyCount,
        };
        ItemAddHintNotify itemAddHintNotify = new ItemAddHintNotify()
        {
            Reason = 4, // ActionReasonShop
        };
        itemAddHintNotify.ItemLists.Add(new ItemHint()
        {
            ItemId = req.Goods.GoodsItem.ItemId,
            Count = req.Goods.GoodsItem.Count,
            IsNew = true,
        });
        session.SendPacket(itemAddHintNotify);
        // we wont add it to inventory for now, as we already have everything
        session.SendPacket(rsp);
    }
}