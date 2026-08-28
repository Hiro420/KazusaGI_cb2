using KazusaGI_cb2.Resource.Excel;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

public class PlayerItem
{
	private Session Session { get; set; } // just in case ill need it
	private MaterialExcelConfig MaterialExcel { get; set; }
	public ulong Guid { get; set; }
	public uint ItemId { get; set; }
	public uint Count { get; set; }

	public PlayerItem(Session session, uint materialId, ulong? overrideGuid = null)
	{
		this.Session = session;
		this.MaterialExcel = MainApp.resourceManager.MaterialExcel[materialId];
		this.Guid = overrideGuid ?? MainApp.GuidMgr.GenGuid(GuidMgr.GuidType.Item);
		this.ItemId = materialId;
		this.Count = 1;
	}

	// more stuff later ig?
}
