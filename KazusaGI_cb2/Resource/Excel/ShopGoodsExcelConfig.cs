namespace KazusaGI_cb2.Resource.Excel;

public class ShopGoodsExcelConfig
{
	public uint goodsId;
	public uint itemId;
	public uint itemCount;
	public uint costScoin;
	public uint costHcoin;
	public List<IdCountConfig> costItems;
	public uint buyLimit;
	public uint refreshDays;
	public string beginTime;
	public string endTime;
	public bool isBuyOnce;
}