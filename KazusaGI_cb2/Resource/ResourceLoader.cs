using KazusaGI_cb2.Resource.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NLua;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using System.Resources;
using KazusaGI_cb2.GameServer.Lua;

namespace KazusaGI_cb2.Resource;

public class ResourceLoader
{
    public static readonly string ExcelSubPath = "ExcelBinOutput";
    public static readonly string JsonSubPath = "BinOutput";
    public static readonly string LuaSubPath = "Lua";
    public string _baseResourcePath;
    private ResourceManager _resourceManager;

    private Dictionary<uint, AvatarExcelConfig> LoadAvatarExcel() =>
        JsonConvert.DeserializeObject<List<AvatarExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "AvatarExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    private Dictionary<uint, AvatarSkillDepotExcelConfig> LoadAvatarSkillDepotExcel() =>
        JsonConvert.DeserializeObject<List<AvatarSkillDepotExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "AvatarSkillDepotExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    private Dictionary<uint, GachaExcel> LoadGachaExcel() =>
        JsonConvert.DeserializeObject<List<GachaExcel>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "GachaExcelConfigData.json"))
        )!.ToDictionary(data => data.sortId);

    private Dictionary<uint, List<GachaPoolExcel>> LoadGachaPoolExcel() =>
        JsonConvert.DeserializeObject<List<GachaPoolExcel>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "GachaPoolExcelConfigData.json"))
        )!.GroupBy(data => data.poolRootId)
        .ToDictionary(
            group => group.Key,
            group => group.ToList()
        );
    private Dictionary<uint, TowerLevelExcelConfig> LoadTowerLevelExcelConfig() =>
        JsonConvert.DeserializeObject<List<TowerLevelExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "TowerLevelExcelConfigData.json"))
        )!.ToDictionary(data => data.levelId);
    private Dictionary<uint, TowerScheduleExcelConfig> LoadTowerScheduleExcelConfig() =>
        JsonConvert.DeserializeObject<List<TowerScheduleExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "TowerScheduleExcelConfigData.json"))
        )!.ToDictionary(data => data.scheduleId);
    private Dictionary<uint, TowerFloorExcelConfig> LoadTowerFloorExcelConfig() =>
        JsonConvert.DeserializeObject<List<TowerFloorExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "TowerFloorExcelConfigData.json"))
        )!.ToDictionary(data => data.floorId);
    private Dictionary<uint, InvestigationTargetConfig> LoadInvestigationTargetConfig() =>
        JsonConvert.DeserializeObject<List<InvestigationTargetConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "InvestigationTargetConfigData.json"))
        )!.ToDictionary(data => data.questId);
    private Dictionary<uint, InvestigationConfig> LoadInvestigationConfig() =>
        JsonConvert.DeserializeObject<List<InvestigationConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "InvestigationConfigData.json"))
        )!.ToDictionary(data => data.id);
    private Dictionary<uint, InvestigationDungeonConfig> LoadInvestigationDungeonConfig() =>
        JsonConvert.DeserializeObject<List<InvestigationDungeonConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "InvestigationDungeonConfigData.json"))
        )!.ToDictionary(data => data.entranceId);
    private Dictionary<uint, InvestigationMonsterConfig> LoadInvestigationMonsterConfig() =>
        JsonConvert.DeserializeObject<List<InvestigationMonsterConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "InvestigationMonsterConfigData.json"))
        )!.ToDictionary(data => data.id);
    private Dictionary<uint, DailyDungeonConfig> LoadDailyDungeonConfig() =>
        JsonConvert.DeserializeObject<List<DailyDungeonConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "DailyDungeonConfigData.json"))
        )!.ToDictionary(data => data.id);
    private Dictionary<uint, DungeonExcelConfig> LoadDungeonExcelConfig() =>
        JsonConvert.DeserializeObject<List<DungeonExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "DungeonExcelConfigData.json"))
        )!.ToDictionary(data => data.id);
    private Dictionary<uint, ShopGoodsExcelConfig> LoadShopGoodsExcelConfig() =>
        JsonConvert.DeserializeObject<List<ShopGoodsExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ShopGoodsExcelConfigData.json"))
        )!.ToDictionary(data => data.goodsId);
    private Dictionary<uint, ShopPlanExcelConfig> LoadShopPlanExcelConfig() =>
        JsonConvert.DeserializeObject<List<ShopPlanExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ShopPlanExcelConfigData.json"))
        )!.ToDictionary(data => data.Id);
    private Dictionary<uint, AvatarSkillExcelConfig> LoadAvatarSkillExcel() =>
        JsonConvert.DeserializeObject<List<AvatarSkillExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "AvatarSkillExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    private Dictionary<uint, MaterialExcelConfig> LoadMaterialExcel() =>
        JsonConvert.DeserializeObject<List<MaterialExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "MaterialExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    private Dictionary<uint, GadgetExcelConfig> LoadGadgetExcel() =>
        JsonConvert.DeserializeObject<List<GadgetExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "GadgetExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    private Dictionary<uint, AvatarCurveExcelConfig> LoadAvatarCurveExcelConfig() =>
        JsonConvert.DeserializeObject<List<AvatarCurveExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "AvatarCurveExcelConfigData.json"))
        )!.ToDictionary(data => data.level);

    private Dictionary<uint, WorldLevelExcelConfig> LoadWorldLevelExcel() =>
        JsonConvert.DeserializeObject<List<WorldLevelExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "WorldLevelExcelConfigData.json"))
        )!.ToDictionary(data => data.level);

    private Dictionary<uint, WeaponCurveExcelConfig> LoadWeaponCurveExcelConfig() =>
        JsonConvert.DeserializeObject<List<WeaponCurveExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "WeaponCurveExcelConfigData.json"))
        )!.ToDictionary(data => data.level);

    private Dictionary<uint, MonsterCurveExcelConfig> LoadMonsterCurveExcelConfig() =>
        JsonConvert.DeserializeObject<List<MonsterCurveExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "MonsterCurveExcelConfigData.json"))
        )!.ToDictionary(data => data.level);

    private Dictionary<uint, Dictionary<uint, ProudSkillExcelConfig>> LoadProudSkillExcel() =>
        JsonConvert.DeserializeObject<List<ProudSkillExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ProudSkillExcelConfigData.json"))
        )!.GroupBy(data => data.proudSkillId)
        .ToDictionary(
            group => group.Key,
            group => group.ToDictionary(config => config.level)
        );

    private Dictionary<uint, MonsterExcelConfig> loadMonsterExcel() =>
        JsonConvert.DeserializeObject<List<MonsterExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "MonsterExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    private Dictionary<uint, WeaponExcelConfig> LoadWeaponExcel() =>
        JsonConvert.DeserializeObject<List<WeaponExcelConfig>>(
            File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "WeaponExcelConfigData.json"))
        )!.ToDictionary(data => data.id);

    // load scene infos asyncronously to speed up loading
    private async Task<Dictionary<uint, ScenePoint>> LoadScenePointsAsync()
    {
        var scenePoints = new Dictionary<uint, ScenePoint>();
        var sceneTasks = new List<Task>();

        string scenePath = Path.Combine(_baseResourcePath, LuaSubPath, "Scene");

        foreach (var sceneDir in Directory.GetDirectories(scenePath))
        {
            string sceneIdStr = Path.GetFileName(sceneDir);
            if (!uint.TryParse(sceneIdStr, out uint sceneId))
                continue;

            string jsonPath = Path.Combine(sceneDir, $"scene{sceneId}_point.json");
            if (File.Exists(jsonPath))
            {
                var scenePoint = JsonConvert.DeserializeObject<ScenePoint>(File.ReadAllText(jsonPath));
                if (scenePoint != null)
                    scenePoints[sceneId] = scenePoint;
            }

            sceneTasks.Add(Task.Run(() => LoadSceneLua(sceneDir, sceneId)));
        }

        await Task.WhenAll(sceneTasks);

        return scenePoints;
    }

    private void LoadSceneLua(string sceneDir, uint sceneId)
    {
        string luaPath = Path.Combine(sceneDir, $"scene{sceneId}.lua");
        using (Lua luaContent = new Lua())
        {
            luaContent.DoString(File.ReadAllText(luaPath));
            SceneLua sceneLuaConfig = new SceneLua();
            LuaTable blocks = (LuaTable)luaContent["blocks"];
            LuaTable scene_config = (LuaTable)luaContent["scene_config"];
            LuaTable dummy_points = (LuaTable)luaContent["dummy_points"];
            LuaTable routes_config = (LuaTable)luaContent["routes_config"];
            Vector3 begin_pos = Table2Vector3(scene_config["begin_pos"]);
            Vector3 born_pos = Table2Vector3(scene_config["born_pos"]);
            Vector3 born_rot = Table2Vector3(scene_config["born_rot"]);
            Vector3 size = Table2Vector3(scene_config["size"]);

            LuaTable block_rects = (LuaTable)luaContent["block_rects"];

            SceneConfig sceneConfig = new SceneConfig()
            {
                begin_pos = begin_pos,
                size = size,
                born_pos = FixSpawnPlayerY(born_pos),
                born_rot = born_rot,
                die_y = Convert.ToInt32(scene_config["die"]),
            };

            sceneLuaConfig.scene_config = sceneConfig;

            sceneLuaConfig.blocks = blocks.Keys.Count > 0
                ? blocks.Values.Cast<object>().Select(block => Convert.ToInt32(block)).ToList()
                : new List<int>();

            sceneLuaConfig.block_rects = new List<BlockRect>();
            if (block_rects != null)
            {
                foreach (LuaTable c in block_rects.Values.Cast<LuaTable>())
                {
                    sceneLuaConfig.block_rects.Add(new BlockRect()
                    {
                        min = Table2Vector3(c["min"]),
                        max = Table2Vector3(c["max"])
                    });
                }
                sceneLuaConfig.scene_blocks = new Dictionary<int, SceneBlockLua>();
                LoadSceneBlock(sceneDir, sceneId, sceneLuaConfig);
            }

            if (dummy_points != null)
            {
                sceneLuaConfig.dummy_points = dummy_points.Values.Count > 0
                ? dummy_points.Values.Cast<string>().ToList()
                : new List<string>();
            }

            if (routes_config != null)
            {
                sceneLuaConfig.routes_config = routes_config.Values.Count > 0
                    ? routes_config.Values.Cast<string>().ToList()
                    : new List<string>();
            }

            _resourceManager.SceneLuas[sceneId] = sceneLuaConfig;
        }
    }

    private void LoadSceneBlock(string sceneDir, uint sceneId, SceneLua sceneLuaConfig)
    {
        Logger logger = new("SceneBlock Loader");
        for (int i = 0; i < sceneLuaConfig.blocks.Count; i++)
        {
            SceneBlockLua sceneBlockLua = new SceneBlockLua();
            Vector3 minPos = sceneLuaConfig.block_rects[i].min;
            Vector3 maxPos = sceneLuaConfig.block_rects[i].max;
            int blockId = sceneLuaConfig.blocks[i];
            string blockLuaPath = Path.Combine(sceneDir, $"scene{sceneId}_block{blockId}.lua");
            using (Lua blockLua = new())
            {
                blockLua.DoString(File.ReadAllText(blockLuaPath));
                sceneBlockLua.groups = new List<SceneGroupBasicLua>();
                sceneBlockLua.scene_groups = new Dictionary<uint, SceneGroupLua>();
                LuaTable groups = (LuaTable)blockLua["groups"];
                foreach (LuaTable group in groups.Values.Cast<LuaTable>())
                {
                    uint groupId = Convert.ToUInt32(group["id"]);
                    SceneGroupBasicLua sceneGroupBasicLua = new SceneGroupBasicLua()
                    {
                        id = groupId,
                        refresh_id = Convert.ToUInt32(group["refresh_id"]),
                        area = Convert.ToUInt32(group["area"]),
                        pos = Table2Vector3(group["pos"]),
                        dynamic_load = Convert.ToBoolean(group["dynamic_load"]),
                        unload_when_disconnect = Convert.ToBoolean(group["unload_when_disconnect"])
                    };
                    sceneBlockLua.groups.Add(sceneGroupBasicLua);
                    string groupLuaPath = Path.Combine(sceneDir, $"scene{sceneId}_group{sceneGroupBasicLua.id}.lua");
                    string mainLuaString = LuaManager.GetCommonScriptConfigAsLua() + "\n"
                        + LuaManager.GetConfigEntityTypeEnumAsLua() + "\n"
                        + LuaManager.GetConfigEntityEnumAsLua() + "\n"
                        + File.ReadAllText(groupLuaPath);

                    sceneBlockLua.scene_groups.Add(sceneGroupBasicLua.id, LoadSceneGroup(mainLuaString, blockId, groupId));
                }
            };
            sceneLuaConfig.scene_blocks[blockId] = sceneBlockLua;
        }
    }

    public SceneGroupLua LoadSceneGroup(string LuaFileContents, int blockId, uint groupId)
    {
        SceneGroupLua sceneGroupLua_ = new SceneGroupLua();
        using (Lua sceneGroupLua = new Lua())
        {
            sceneGroupLua.DoString(LuaFileContents);
            LuaTable monstersList = (LuaTable)sceneGroupLua["monsters"];
            LuaTable gadgetsList = (LuaTable)sceneGroupLua["gadgets"];
            LuaTable npcsList = (LuaTable)sceneGroupLua["npcs"];
            LuaTable initConfig = (LuaTable)sceneGroupLua["init_config"];
            LuaTable suites = (LuaTable)sceneGroupLua["suites"];
            LuaTable triggers_config = (LuaTable)sceneGroupLua["triggers"];
            sceneGroupLua_.monsters = new List<MonsterLua>();
            sceneGroupLua_.triggers = new List<SceneTriggerLua>();
            sceneGroupLua_.npcs = new List<NpcLua>();
            sceneGroupLua_.gadgets = new List<GadgetLua>();
            sceneGroupLua_.init_config = new SceneGroupLuaInitConfig();
            sceneGroupLua_.suites = new List<SceneGroupLuaSuite>();


            foreach (LuaTable trigger in triggers_config.Values.Cast<LuaTable>())
            {
                SceneTriggerLua triggerLua = new SceneTriggerLua()
                {
                    name = Convert.ToString(trigger["name"])!,
                    action = Convert.ToString(trigger["action"])!,
                    condition = Convert.ToString(trigger["condition"])!,
                };

                if (trigger["event"] != null)
                {
                    triggerLua._event = (EventTriggerType)Convert.ToUInt32(trigger["event"]);
                }
                sceneGroupLua_.triggers.Add(triggerLua);
            }

            foreach (LuaTable monster in monstersList.Values.Cast<LuaTable>())
            {
                MonsterLua monsterLua = new MonsterLua()
                {
                    monster_id = Convert.ToUInt32(monster["monster_id"]),
                    config_id = Convert.ToUInt32(monster["config_id"]),
                    level = Convert.ToUInt32(monster["level"]),
                    pose_id = Convert.ToUInt32(monster["pose_id"]),
                    isElite = Convert.ToBoolean(monster["isElite"]),
                    pos = Table2Vector3(monster["pos"]),
                    rot = Table2Vector3(monster["rot"]),
                    affix = monster["affix"] != null
                        ? new List<uint>(((LuaTable)monster["affix"]).Values.Cast<object>().Select(v => Convert.ToUInt32(v)))
                        : new List<uint>(),
                    block_id = Convert.ToUInt32(blockId),
                    group_id = groupId

                };
                sceneGroupLua_.monsters.Add(monsterLua);
            }

            foreach (LuaTable npc in npcsList.Values.Cast<LuaTable>())
            {
                sceneGroupLua_.npcs.Add(new NpcLua()
                {
                    config_id = Convert.ToUInt32(npc["config_id"]),
                    npc_id = Convert.ToUInt32(npc["npc_id"]),
                    pos = Table2Vector3(npc["pos"]),
                    rot = Table2Vector3(npc["rot"]),
                    block_id = Convert.ToUInt32(blockId),
                    group_id = groupId
                });
            }

            foreach (LuaTable gadget in gadgetsList.Values.Cast<LuaTable>())
            {
                sceneGroupLua_.gadgets.Add(new GadgetLua()
                {
                    config_id = Convert.ToUInt32(gadget["config_id"]),
                    gadget_id = Convert.ToUInt32(gadget["gadget_id"]),
                    pos = FixGadgetY(Table2Vector3(gadget["pos"])),
                    rot = Table2Vector3(gadget["rot"]),
                    route_id = Convert.ToUInt32(gadget["route_id"]),
                    level = Convert.ToUInt32(gadget["level"]),
                    block_id = Convert.ToUInt32(blockId),
                    group_id = groupId,
                    state = gadget["state"] != null ? (GadgetState)Convert.ToUInt32(gadget["state"]) : GadgetState.Default,
                    type = gadget["type"] != null ? (GadgetType_Lua)Convert.ToUInt32(gadget["type"]) : GadgetType_Lua.GADGET_NONE
                });
            }

            sceneGroupLua_.init_config.suite = Convert.ToUInt32(initConfig["suite"]);

            foreach (LuaTable suite in suites.Values.Cast<LuaTable>())
            {
                SceneGroupLuaSuite sceneGroupLuaSuite = new SceneGroupLuaSuite()
                {
                    monsters = suite["monsters"] != null
                        ? new List<uint>(((LuaTable)suite["monsters"]).Values.Cast<object>().Select(v => Convert.ToUInt32(v)))
                        : new List<uint>(),

                    gadgets = suite["gadgets"] != null
                        ? new List<uint>(((LuaTable)suite["gadgets"]).Values.Cast<object>().Select(v => Convert.ToUInt32(v)))
                        : new List<uint>(),

                    regions = suite["regions"] != null
                        ? new List<uint>(((LuaTable)suite["regions"]).Values.Cast<object>().Select(v => Convert.ToUInt32(v)))
                        : new List<uint>(),

                    triggers = suite["triggers"] != null
                        ? new List<string>(((LuaTable)suite["triggers"]).Values.Cast<object>().Select(v => v.ToString())!)
                        : new List<string>(),

                    rand_weight = Convert.ToUInt32(suite["rand_weight"])
                };

                sceneGroupLua_.suites.Add(sceneGroupLuaSuite);
            }
        }
        return sceneGroupLua_;
    }

    private Vector3 FixGadgetY(Vector3 pos)
    {
        pos.Y -= 1.0F; // :skull:
        return pos;
    }

    private Vector3 FixSpawnPlayerY(Vector3 pos)
    {
        pos.Y += 0.3F; // :skull:
        return pos;
    }

    private Vector3 Table2Vector3(object vectorTable)
    {
        LuaTable _vectorTable = (LuaTable)vectorTable;
        return new Vector3()
        {
            X = Convert.ToSingle(Convert.ToDouble(_vectorTable["x"])),
            Y = _vectorTable["y"] != null ? Convert.ToSingle(Convert.ToDouble(_vectorTable["y"])) : 0.0F,
            Z = Convert.ToSingle(Convert.ToDouble(_vectorTable["z"]))
        };
    }

    public ResourceLoader(ResourceManager resourceManager, string baseResourcePath)
    {
        _baseResourcePath = baseResourcePath;
        this._resourceManager = resourceManager;
        _resourceManager.SceneLuas = new Dictionary<uint, SceneLua>();
        _resourceManager.AvatarExcel = this.LoadAvatarExcel();
        _resourceManager.AvatarSkillDepotExcel = this.LoadAvatarSkillDepotExcel();
        _resourceManager.AvatarSkillExcel = this.LoadAvatarSkillExcel();
        _resourceManager.ProudSkillExcel = this.LoadProudSkillExcel();
        _resourceManager.WeaponExcel = this.LoadWeaponExcel();
        _resourceManager.ScenePoints = LoadScenePointsAsync().Result;
        _resourceManager.MonsterExcel = this.loadMonsterExcel();
        _resourceManager.GadgetExcel = this.LoadGadgetExcel();
        _resourceManager.MaterialExcel = this.LoadMaterialExcel();
        _resourceManager.GachaExcel = this.LoadGachaExcel();
        _resourceManager.GachaPoolExcel = this.LoadGachaPoolExcel();
        _resourceManager.AvatarCurveExcel = this.LoadAvatarCurveExcelConfig();
        _resourceManager.WeaponCurveExcel = this.LoadWeaponCurveExcelConfig();
        _resourceManager.WorldLevelExcel = this.LoadWorldLevelExcel();
        _resourceManager.MonsterCurveExcel = this.LoadMonsterCurveExcelConfig();
        _resourceManager.ShopGoodsExcel = this.LoadShopGoodsExcelConfig();
        _resourceManager.ShopPlanExcel = this.LoadShopPlanExcelConfig();
        _resourceManager.DungeonExcel = this.LoadDungeonExcelConfig();
        _resourceManager.DailyDungeonExcel = this.LoadDailyDungeonConfig();
        _resourceManager.InvestigationExcel = this.LoadInvestigationConfig();
        _resourceManager.InvestigationTargetExcel = this.LoadInvestigationTargetConfig();
        _resourceManager.InvestigationDungeonExcel = this.LoadInvestigationDungeonConfig();
        _resourceManager.InvestigationMonsterExcel = this.LoadInvestigationMonsterConfig();
        _resourceManager.TowerFloorExcel = this.LoadTowerFloorExcelConfig();
        _resourceManager.TowerScheduleExcel = this.LoadTowerScheduleExcelConfig();
        _resourceManager.TowerLevelExcel = this.LoadTowerLevelExcelConfig();
    }
}
