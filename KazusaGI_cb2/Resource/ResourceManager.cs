using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Avatar;
using KazusaGI_cb2.Resource.Json.Scene;
using KazusaGI_cb2.Resource.Json.Talent;
using KazusaGI_cb2.Utils;

namespace KazusaGI_cb2.Resource;

public class ResourceManager
{
    public ResourceLoader loader;
    public Dictionary<uint, AvatarExcelConfig> AvatarExcel { get; set; }
    public Dictionary<uint, AvatarSkillDepotExcelConfig> AvatarSkillDepotExcel { get; set; }
    public Dictionary<uint, AvatarSkillExcelConfig> AvatarSkillExcel { get; set; }
	public Dictionary<uint, AvatarTalentExcelConfig> AvatarTalentExcel { get; set; }
	public Dictionary<uint, ProudSkillExcelConfig> ProudSkillExcel { get; set; }
    public Dictionary<uint, WeaponExcelConfig> WeaponExcel { get; set; }
    public Dictionary<uint, MonsterExcelConfig> MonsterExcel { get; set; }
    public Dictionary<uint, GadgetExcelConfig> GadgetExcel { get; set; }
    public ConcurrentDictionary<uint, ScenePoint> ScenePoints { get; set; }
    public ConcurrentDictionary<uint, SceneLua> SceneLuas { get; set; }
    public Dictionary<uint, MaterialExcelConfig> MaterialExcel { get; set; }
    public Dictionary<uint, GachaExcel> GachaExcel { get; set; }
    public Dictionary<uint, List<GachaPoolExcel>> GachaPoolExcel { get; set; }
    public Dictionary<uint, AvatarCurveExcelConfig> AvatarCurveExcel { get; set; }
    public Dictionary<uint, WeaponCurveExcelConfig> WeaponCurveExcel { get; set; }
    public Dictionary<uint, WorldLevelExcelConfig> WorldLevelExcel { get; set; }
    public Dictionary<uint, MonsterCurveExcelConfig> MonsterCurveExcel { get; set; }
    public Dictionary<uint, ShopGoodsExcelConfig> ShopGoodsExcel { get; set; }
    public Dictionary<uint, ShopPlanExcelConfig> ShopPlanExcel { get; set; }
    public Dictionary<uint, DungeonExcelConfig> DungeonExcel { get; set; }
    public Dictionary<uint, InvestigationConfig> InvestigationExcel { get; set; }
    public Dictionary<uint, InvestigationTargetConfig> InvestigationTargetExcel { get; set; }
    public Dictionary<uint, InvestigationDungeonConfig> InvestigationDungeonExcel { get; set; }
    public Dictionary<uint, InvestigationMonsterConfig> InvestigationMonsterExcel { get; set; }
    public Dictionary<uint, DailyDungeonConfig> DailyDungeonExcel { get; set; }
    public Dictionary<uint, TowerFloorExcelConfig> TowerFloorExcel { get; set; }
    public Dictionary<uint, TowerScheduleExcelConfig> TowerScheduleExcel { get; set; }
    public Dictionary<uint, TowerLevelExcelConfig> TowerLevelExcel { get; set; }
    public Dictionary<uint, string> GadgetLuaConfig { get; set; }
    public GlobalCombatData GlobalCombatData { get; set; }


	public ConcurrentDictionary<string, Dictionary<string, BaseConfigTalent[]>> AvatarTalentConfigDataMap { get; set; } // file name
	public Dictionary<string, ConfigAbilityContainer> ConfigAbilityMap { get; set; } // ability name
    public Dictionary<string, ConfigAvatar> ConfigAvatarMap { get; set; }
	public Dictionary<string, ConfigGadget> ConfigGadgetMap { get; set; }


	public ResourceManager(string baseResourcePath = "resources")
    {
        // Init Logger
        Logger c = new("ResourceLoader");

        // :3
        c.LogInfo("Loading Resources, this may take a while..");

        // Load all resources here
        this.loader = new(this, baseResourcePath);

        // Log SUCCESS
        c.LogSuccess("Loaded Resources");
	}

    public string GetLuaStringFromGroupId(uint groupId)
    {
        string directory = Path.Combine(this.loader._baseResourcePath, ResourceLoader.LuaSubPath, "Scene");
        return Directory.GetFiles(directory, $"*_group{groupId}.lua", SearchOption.AllDirectories).FirstOrDefault()!;
    }
}
