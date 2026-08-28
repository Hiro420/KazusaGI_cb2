namespace KazusaGI_cb2.Resource.Excel;

public class DungeonExcelConfig
{
	public uint id;
	public uint nameTextMapHash;
	public uint descTextMapHash;
	public DungeonType type;
	public bool isDynamicLevel;
	public uint serialId;
	// public DungeonPlayType playType;
	public uint sceneId;
	public uint eventInterval;
	public uint firstDropPreview;
	public uint dropPreview;
	// public InvolveType involveType;
	public uint showLevel;
	public uint avatarLimitType;
	public uint limitLevel;
	public int levelRevise;
	public uint prevDungeonId;
	public uint requireAvatarId;
	public uint passCond;
	public uint passJumpDungeon;
	public uint reviveMaxCount;
	public uint dayEnterCount;
	public List<IdCountConfig> enterCostItems;
	public uint firstPassRewardId;
	public uint passRewardId;
	public uint settleCountdownTime;
	// public List<SettleShowType> settleShows;
	public bool forbiddenRestart;
	// public SettleUIType settleUIType;
	public uint statueCostID;
	public uint statueCostCount;
	public uint statueDrop;
	// public List<ElementType> recommendElementTypes;
	public Dictionary<uint, uint> levelConfigMap;
	public uint cityID;
	// public PIMFAEMLAJF IPBLCIEPDPE;
	public bool dontShowPushTips;
}