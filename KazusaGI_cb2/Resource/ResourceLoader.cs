using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json;
using KazusaGI_cb2.Resource.Json.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.AbilityPath;
using KazusaGI_cb2.Resource.Json.Avatar;
using KazusaGI_cb2.Resource.Json.Level;
using KazusaGI_cb2.Resource.Json.Preload;
using KazusaGI_cb2.Resource.Json.Scene;
using KazusaGI_cb2.Resource.Json.Talent;
using KazusaGI_cb2.Resource.ServerExcel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLua;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

namespace KazusaGI_cb2.Resource;

public class ResourceLoader
{
	public static readonly string ExcelSubPath = "ExcelBinOutput";
	public static readonly string JsonSubPath = "BinOutput";
	public static readonly string LuaSubPath = "Lua";
	public static readonly string ServerExcelSubPath = "ServerExcelOutput";
	public string _baseResourcePath;
	private ResourceManager _resourceManager;
	private static Logger logger1 = new("ResourceLoader");

	public string LuaPath => Path.Combine(_baseResourcePath, LuaSubPath);

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
	private Dictionary<uint, DungeonChallengeConfig> LoadDungeonChallengeConfig() =>
		JsonConvert.DeserializeObject<List<DungeonChallengeConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "DungeonChallengeConfigData.json"))
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
	private Dictionary<uint, AvatarTalentExcelConfig> LoadAvatarTalentExcelConfig() =>
		JsonConvert.DeserializeObject<List<AvatarTalentExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "AvatarTalentExcelConfigData.json"))
		)!.ToDictionary(data => data.talentId);

	private Dictionary<uint, MaterialExcelConfig> LoadMaterialExcel() =>
		JsonConvert.DeserializeObject<List<MaterialExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "MaterialExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, GatherExcelConfig> LoadGatherExcelConfig() =>
		JsonConvert.DeserializeObject<List<GatherExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "GatherExcelConfigData.json"))
		)!.ToDictionary(data => data.pointType);

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

	private Dictionary<uint, ProudSkillExcelConfig> LoadProudSkillExcel() =>
		JsonConvert.DeserializeObject<List<ProudSkillExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ProudSkillExcelConfigData.json"))
		)!.ToDictionary(
			group => group.proudSkillId,
			group => group
		);

	private Dictionary<uint, MonsterExcelConfig> loadMonsterExcel() =>
		JsonConvert.DeserializeObject<List<MonsterExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "MonsterExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, WeaponExcelConfig> LoadWeaponExcel() =>
		JsonConvert.DeserializeObject<List<WeaponExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "WeaponExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, Dictionary<uint, WeaponPromoteExcelConfig>> LoadWeaponPromoteExcelConfig() =>
		JsonConvert.DeserializeObject<List<WeaponPromoteExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "WeaponPromoteExcelConfigData.json"))
		)!.GroupBy(data => data.weaponPromoteId)
		.ToDictionary(
			group => group.Key,
			group => group.ToDictionary(data => data.promoteLevel)
		);

	private Dictionary<uint, ReliquaryMainPropExcelConfig> LoadReliquaryMainPropExcelConfig() =>
		JsonConvert.DeserializeObject<List<ReliquaryMainPropExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ReliquaryMainPropExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, ReliquaryAffixExcelConfig> LoadReliquaryAffixExcelConfig() =>
		JsonConvert.DeserializeObject<List<ReliquaryAffixExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ReliquaryAffixExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, ReliquaryExcelConfig> LoadReliquaryExcelConfig() =>
		JsonConvert.DeserializeObject<List<ReliquaryExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "ReliquaryExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, EquipAffixExcelConfig> LoadEquipAffixExcel() =>
		JsonConvert.DeserializeObject<List<EquipAffixExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "EquipAffixExcelConfigData.json"))
		)!.ToDictionary(data => data.AffixId);

	private Dictionary<uint, MonsterAffixExcelConfig> LoadMonsterAffixExcel() =>
		JsonConvert.DeserializeObject<List<MonsterAffixExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "MonsterAffixExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private Dictionary<uint, SceneExcelConfig> LoadSceneExcel() =>
		JsonConvert.DeserializeObject<List<SceneExcelConfig>>(
			File.ReadAllText(Path.Combine(_baseResourcePath, ExcelSubPath, "SceneExcelConfigData.json"))
		)!.ToDictionary(data => data.id);

	private GlobalCombatData LoadGlobalCombatData() =>
		JsonConvert.DeserializeObject<GlobalCombatData>(
			File.ReadAllText(Path.Combine(_baseResourcePath, JsonSubPath, "Common", "ConfigGlobalCombat.json"))
		)!;

	private ConfigPreload LoadConfigPreload() =>
		JsonConvert.DeserializeObject<ConfigPreload>(
			File.ReadAllText(Path.Combine(_baseResourcePath, JsonSubPath, "Preload", "ConfigPreload.json"))
		)!;

	private AbilityPathData LoadAbilityPathData() =>
		JsonConvert.DeserializeObject<AbilityPathData>(
			File.ReadAllText(Path.Combine(_baseResourcePath, JsonSubPath, "AbilityPath", "AbilityPathData.json"))
		)!;

	private ConcurrentDictionary<string, BaseConfigTalent[]> LoadTalentConfigs()
	{
		ConcurrentDictionary<string, BaseConfigTalent[]> ret = new();

		string[] filePaths = Directory.GetFiles(
			Path.Combine(_baseResourcePath, JsonSubPath, "Talent", "AvatarTalents"),
			"*.json", SearchOption.AllDirectories
		);
		var tasks = new List<Task>();
		filePaths.AsParallel().ForAll(async file =>
		{
			var filePath = new FileInfo(file);
			using var sr = new StringReader(await File.ReadAllTextAsync(filePath.FullName));
			using var jr = new JsonTextReader(sr);
			var fileData = Serializer.Deserialize<Dictionary<string, BaseConfigTalent[]>>(jr) ?? new();
			foreach (var kv in fileData)
			{
				ret[kv.Key] = kv.Value;
			}
		});

		return ret;
	}

	// load scene infos asyncronously to speed up loading
	private async Task<ConcurrentDictionary<uint, ScenePoint>> LoadScenePointsAsync()
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

		return new ConcurrentDictionary<uint, ScenePoint>(scenePoints);
	}

	public Dictionary<string, ConfigAbilityContainer> LoadConfigAbilityMap()
	{
		var ret = new Dictionary<string, ConfigAbilityContainer>(StringComparer.Ordinal);

		string[] filePaths = Directory.GetFiles(
			Path.Combine(_baseResourcePath, JsonSubPath, "Ability", "Temp"),
			"*.json", SearchOption.AllDirectories
		);
		Array.Sort(filePaths, StringComparer.Ordinal);

		foreach (string file in filePaths)
		{
			ConfigAbilityContainer[] fileData;
			try
			{
				using var sr = new StreamReader(file);
				using var jr = new JsonTextReader(sr)
				{
					DateParseHandling = DateParseHandling.None,
					FloatParseHandling = FloatParseHandling.Double
				};
				fileData = Serializer.Deserialize<ConfigAbilityContainer[]>(jr)
					?? throw new JsonSerializationException("Ability file deserialized to null.");
			}
			catch (AbilityConfigLoadException)
			{
				throw;
			}
			catch (JsonReaderException e)
			{
				throw new AbilityConfigLoadException(file, e.Message, e.Path, e);
			}
			catch (JsonSerializationException e)
			{
				throw new AbilityConfigLoadException(file, e.Message, null, e);
			}
			catch (Exception e)
			{
				throw new AbilityConfigLoadException(file, e.Message, null, e);
			}

			for (int i = 0; i < fileData.Length; i++)
			{
				ConfigAbilityContainer container = fileData[i]
					?? throw new AbilityConfigLoadException(file, $"Null ability container at array index {i}.", $"[{i}]");

				if (container.Default is not ConfigAbility ability)
				{
					throw new AbilityConfigLoadException(
						file,
						$"Default must resolve to ConfigAbility, got '{container.Default?.GetType().FullName ?? "null"}'.",
						$"[{i}].Default");
				}

				if (string.IsNullOrEmpty(ability.abilityName))
					throw new AbilityConfigLoadException(file, "ConfigAbility.abilityName is empty.", $"[{i}].Default.abilityName");

				try
				{
					ability.Bake();
				}
				catch (Exception e)
				{
					throw new AbilityConfigLoadException(file, $"Bake failed for ability '{ability.abilityName}': {e.Message}", $"[{i}].Default", e);
				}

				if (!ret.TryAdd(ability.abilityName, container))
					throw new AbilityConfigLoadException(file, $"Duplicate ability name '{ability.abilityName}'.", $"[{i}].Default.abilityName");
			}
		}

		logger1.LogSuccess($"Loaded {ret.Count} abilities.");
		return ret;
	}

	public async Task<Dictionary<string, ConfigGadget>> LoadConfigGadgetMap()
	{
		var ret = new ConcurrentDictionary<string, ConfigGadget>();

		string[] filePaths = Directory.GetFiles(
			Path.Combine(_baseResourcePath, JsonSubPath, "Gadget"),
			"*.json", SearchOption.AllDirectories
		);

		var tasks = filePaths.Select(async file =>
		{
			string data = await File.ReadAllTextAsync(file);
			var configs = JsonConvert.DeserializeObject<Dictionary<string, ConfigGadget>>(data)!;
			foreach (var kv in configs)
			{
				ret[kv.Key] = kv.Value;
			}
		});

		await Task.WhenAll(tasks);

		return ret.ToDictionary();
	}

	public async Task<Dictionary<string, Json.Monster.ConfigMonster>> LoadConfigMonsterMap()
	{
		var ret = new ConcurrentDictionary<string, Json.Monster.ConfigMonster>();

		string[] filePaths = Directory.GetFiles(
			Path.Combine(_baseResourcePath, JsonSubPath, "Monster"),
			"*.json", SearchOption.AllDirectories
		);

		var tasks = filePaths.Select(async file =>
		{
			string data = await File.ReadAllTextAsync(file);
			var config = JsonConvert.DeserializeObject<Json.Monster.ConfigMonster>(data);
			if (config == null)
				return;

			string key = Path.GetFileNameWithoutExtension(file); // e.g. "ConfigMonster_Hili_None_01"
			ret[key] = config;
		});

		await Task.WhenAll(tasks);

		return ret.ToDictionary();
	}


	public Dictionary<string, ConfigLevelEntity> LoadConfigLevelEntityMap()
	{
		var ret = new Dictionary<string, ConfigLevelEntity>(StringComparer.Ordinal);
		string levelDir = Path.Combine(_baseResourcePath, JsonSubPath, "Level");
		if (!Directory.Exists(levelDir))
			return ret;

		string[] filePaths = Directory.GetFiles(levelDir, "*.json", SearchOption.AllDirectories);
		Array.Sort(filePaths, StringComparer.Ordinal);

		foreach (string file in filePaths)
		{
			JToken root = JToken.Parse(File.ReadAllText(file));
			if (root is not JObject obj)
				continue;

			// CB2's level json directory contains named ConfigLevelEntity objects.
			// A direct object is also accepted under its file stem because the
			// SceneExcel levelEntityConfig key names that same config.
			if (IsLevelEntityObject(obj))
			{
				ConfigLevelEntity? config = obj.ToObject<ConfigLevelEntity>();
				if (config != null)
					ret[Path.GetFileNameWithoutExtension(file)] = config;
				continue;
			}

			foreach (JProperty property in obj.Properties())
			{
				if (property.Value is not JObject child || !IsLevelEntityObject(child))
					continue;
				ConfigLevelEntity? config = child.ToObject<ConfigLevelEntity>();
				if (config != null)
					ret[property.Name] = config;
			}
		}

		return ret;

		static bool IsLevelEntityObject(JObject obj)
		{
			string? type = obj.Value<string>("$type");
			return string.Equals(type, "ConfigLevelEntity", StringComparison.Ordinal) ||
				   obj.Property("avatarAbilities") != null ||
				   obj.Property("teamAbilities") != null ||
				   obj.Property("monsterAbilities") != null;
		}
	}

	public Dictionary<string, ConfigAvatar> LoadConfigAvatarMap()
	{
		ConcurrentDictionary<string, ConfigAvatar> ret = new();

		string[] filePaths = Directory.GetFiles(
			Path.Combine(_baseResourcePath, JsonSubPath, "Avatar"),
			"*.json", SearchOption.TopDirectoryOnly
		);
		var tasks = new List<Task>();
		filePaths.AsParallel().ForAll(file =>
		{
			var filePath = new FileInfo(file);
			var fileData = JsonConvert.DeserializeObject<ConfigAvatar>(File.ReadAllText(filePath.FullName))!;
			ret[Regex.Replace(filePath.Name, "\\.json", "")] = fileData;
		});

		return ret.ToDictionary();
	}

	private void LoadSceneLua(string sceneDir, uint sceneId)
	{
		string luaPath = Path.Combine(sceneDir, $"scene{sceneId}.lua");
		using (Lua luaContent = new Lua())
		{
			// Ensure NLua uses UTF-8 so non-ASCII characters (e.g. Chinese drop tags)
			// round-trip correctly between C# and Lua.
			luaContent.State.Encoding = UTF8Encoding.UTF8;

			luaContent.DoString(File.ReadAllText(luaPath, UTF8Encoding.UTF8));
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
				// Use UTF-8 when executing block Lua so any non-ASCII content
				// is preserved inside the Lua VM.
				blockLua.State.Encoding = System.Text.Encoding.UTF8;

				blockLua.DoString(File.ReadAllText(blockLuaPath, blockLua.State.Encoding));
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
						+ File.ReadAllText(groupLuaPath, blockLua.State.Encoding);

					sceneBlockLua.scene_groups.Add(sceneGroupBasicLua.id, LoadSceneGroup(mainLuaString, blockId, groupId));
				}
			}
			;
			sceneLuaConfig.scene_blocks[blockId] = sceneBlockLua;
		}
	}

	public SceneGroupLua LoadSceneGroup(string LuaFileContents, int blockId, uint groupId)
	{
		SceneGroupLua sceneGroupLua_ = new SceneGroupLua();
		using (Lua sceneGroupLua = new Lua())
		{
			// Group scripts define gadget drop_tag strings and other localized
			// content; force UTF-8 so these are not collapsed to question marks.
			sceneGroupLua.State.Encoding = System.Text.Encoding.UTF8;

			sceneGroupLua.DoString(LuaFileContents);
			LuaTable monstersList = (LuaTable)sceneGroupLua["monsters"];
			LuaTable gadgetsList = (LuaTable)sceneGroupLua["gadgets"];
			LuaTable npcsList = (LuaTable)sceneGroupLua["npcs"];
			LuaTable initConfig = (LuaTable)sceneGroupLua["init_config"];
			LuaTable suites = (LuaTable)sceneGroupLua["suites"];
			LuaTable triggers_config = (LuaTable)sceneGroupLua["triggers"];

			// Optional group variables table from Lua: variables = { { name=..., value=..., no_refresh=... }, ... }
			if (sceneGroupLua["variables"] is LuaTable variablesTable)
			{
				foreach (LuaTable var in variablesTable.Values.Cast<LuaTable>())
				{
					var nameObj = var["name"];
					var valueObj = var["value"];
					if (nameObj == null || valueObj == null)
						continue;

					string name = Convert.ToString(nameObj)!;
					int value = Convert.ToInt32(valueObj);

					// Last definition wins if duplicated, which matches hk4e's
					// behavior of simply overwriting the variable.
					sceneGroupLua_.variables[name] = value;
				}
			}
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
					// Lua trigger field: source = "...". hk4e uses this
					// together with the event type to decide whether a
					// trigger matches a given Event (see
					// Group::isTriggerEventMatch). We mirror it so that
					// ScriptArgs.source can be compared against it.
					source = Convert.ToString(trigger["source"]) ?? string.Empty,
				};

				if (trigger["event"] != null)
				{
					triggerLua._event = (EventType)Convert.ToUInt32(trigger["event"]);
				}

				// Lua trigger field: trigger_count = N. In hk4e this is a
				// "max trigger count" cap: 0 means unlimited, positive
				// values are the maximum number of times the trigger is
				// allowed to fire.
				var maxCountObj = trigger["trigger_count"];
				if (maxCountObj != null)
				{
					try
					{
						triggerLua.trigger_count = Convert.ToUInt32(maxCountObj);
					}
					catch
					{
						triggerLua.trigger_count = 0;
					}
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
					type = gadget["type"] != null ? (GadgetType_Lua)Convert.ToUInt32(gadget["type"]) : GadgetType_Lua.GADGET_NONE,
					born_type = gadget["born_type"] != null
						? (KazusaGI_cb2.Protocol.GadgetBornType)Convert.ToUInt32(gadget["born_type"])
						: KazusaGI_cb2.Protocol.GadgetBornType.GadgetBornNone,

					// Optional fields, mirror hk4e behavior: missing/nil -> default
					isOneoff = gadget["isOneoff"] != null && Convert.ToBoolean(gadget["isOneoff"]),
					persistent = gadget["persistent"] != null && Convert.ToBoolean(gadget["persistent"]),
					showcutscene = gadget["showcutscene"] != null && Convert.ToBoolean(gadget["showcutscene"]),
					drop_tag = gadget["drop_tag"] != null ? Convert.ToString(gadget["drop_tag"]) : null,
					interact_id = gadget["interact_id"] != null ? Convert.ToUInt32(gadget["interact_id"]) : 0u,
					mark_flag = gadget["mark_flag"] != null ? Convert.ToUInt32(gadget["mark_flag"]) : 0u,
					point_type = gadget["point_type"] != null ? Convert.ToUInt32(gadget["point_type"]) : 0u,
					owner = gadget["owner"] != null ? Convert.ToUInt32(gadget["owner"]) : 0u
				});
			}

			sceneGroupLua_.init_config.suite = Convert.ToUInt32(initConfig["suite"]);

			try
			{
				var endSuiteObj = initConfig["end_suite"];
				if (endSuiteObj != null)
				{
					sceneGroupLua_.init_config.end_suite = Convert.ToUInt32(endSuiteObj);
				}
			}
			catch
			{
				// end_suite is optional; ignore if missing or invalid
			}

			try
			{
				var randSuiteObj = initConfig["rand_suite"];
				if (randSuiteObj != null)
				{
					if (randSuiteObj is bool b)
						sceneGroupLua_.init_config.rand_suite = b ? 1u : 0u;
					else
						sceneGroupLua_.init_config.rand_suite = Convert.ToUInt32(randSuiteObj);
				}
			}
			catch
			{
				// rand_suite is optional; ignore if missing or invalid
			}

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
		//pos.Y -= 1.0F; // :skull:
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
			X = Convert.ToSingle(_vectorTable["x"]),
			Y = _vectorTable["y"] != null ? Convert.ToSingle(_vectorTable["y"]) : 0.0F,
			Z = Convert.ToSingle(_vectorTable["z"])
		};
	}

	public ResourceLoader(ResourceManager resourceManager, string baseResourcePath)
	{
		_baseResourcePath = baseResourcePath;
		this._resourceManager = resourceManager;
		_resourceManager.SceneLuas = new ConcurrentDictionary<uint, SceneLua>();
		_resourceManager.AvatarExcel = this.LoadAvatarExcel();
		_resourceManager.AvatarSkillDepotExcel = this.LoadAvatarSkillDepotExcel();
		_resourceManager.AvatarSkillExcel = this.LoadAvatarSkillExcel();
		_resourceManager.ProudSkillExcel = this.LoadProudSkillExcel();
		_resourceManager.AvatarTalentExcel = this.LoadAvatarTalentExcelConfig();
		_resourceManager.WeaponExcel = this.LoadWeaponExcel();
		_resourceManager.ScenePoints = LoadScenePointsAsync().Result;
		_resourceManager.MonsterExcel = this.loadMonsterExcel();
		_resourceManager.GadgetExcel = this.LoadGadgetExcel();
		_resourceManager.MaterialExcel = this.LoadMaterialExcel();
		_resourceManager.GatherExcel = this.LoadGatherExcelConfig();
		_resourceManager.GachaExcel = this.LoadGachaExcel();
		_resourceManager.GachaPoolExcel = this.LoadGachaPoolExcel();
		_resourceManager.AvatarCurveExcel = this.LoadAvatarCurveExcelConfig();
		_resourceManager.WeaponCurveExcel = this.LoadWeaponCurveExcelConfig();
		_resourceManager.WorldLevelExcel = this.LoadWorldLevelExcel();
		_resourceManager.MonsterCurveExcel = this.LoadMonsterCurveExcelConfig();
		_resourceManager.ShopGoodsExcel = this.LoadShopGoodsExcelConfig();
		_resourceManager.ShopPlanExcel = this.LoadShopPlanExcelConfig();
		_resourceManager.DungeonExcel = this.LoadDungeonExcelConfig();
		_resourceManager.DungeonChallengeConfig = this.LoadDungeonChallengeConfig();
		_resourceManager.DailyDungeonExcel = this.LoadDailyDungeonConfig();
		_resourceManager.InvestigationExcel = this.LoadInvestigationConfig();
		_resourceManager.InvestigationTargetExcel = this.LoadInvestigationTargetConfig();
		_resourceManager.InvestigationDungeonExcel = this.LoadInvestigationDungeonConfig();
		_resourceManager.InvestigationMonsterExcel = this.LoadInvestigationMonsterConfig();
		_resourceManager.TowerFloorExcel = this.LoadTowerFloorExcelConfig();
		_resourceManager.TowerScheduleExcel = this.LoadTowerScheduleExcelConfig();
		_resourceManager.TowerLevelExcel = this.LoadTowerLevelExcelConfig();
		_resourceManager.WeaponPromoteExcel = this.LoadWeaponPromoteExcelConfig();
		_resourceManager.ReliquaryExcel = this.LoadReliquaryExcelConfig();
		_resourceManager.MonsterAffixExcel = this.LoadMonsterAffixExcel();
		_resourceManager.SceneExcel = this.LoadSceneExcel();
		_resourceManager.ReliquaryMainPropExcel = this.LoadReliquaryMainPropExcelConfig();
		_resourceManager.ReliquaryAffixExcel = this.LoadReliquaryAffixExcelConfig();
		_resourceManager.EquipAffixExcel = this.LoadEquipAffixExcel();
		_resourceManager.GlobalCombatData = this.LoadGlobalCombatData();
		_resourceManager.ConfigPreload = this.LoadConfigPreload();
		_resourceManager.AbilityPathData = this.LoadAbilityPathData();

		_resourceManager.AvatarTalentConfigDataMap = this.LoadTalentConfigs();
		_resourceManager.ConfigAvatarMap = this.LoadConfigAvatarMap();
		_resourceManager.ConfigLevelEntityMap = this.LoadConfigLevelEntityMap();
		_resourceManager.ConfigAbilityMap = this.LoadConfigAbilityMap();
		_resourceManager.ConfigGadgetMap = this.LoadConfigGadgetMap().Result;
		_resourceManager.ConfigMonsterMap = this.LoadConfigMonsterMap().Result;

		_resourceManager.ServerAvatarRows = this.LoadServerExcel<AvatarRow>("AvatarData");
		_resourceManager.ServerMonsterRows = this.LoadServerExcel<MonsterRow>("MonsterData");
		_resourceManager.ServerChestDropRows = this.LoadServerExcel<ChestDropRow>("ChestDropData");
		_resourceManager.ServerMonsterAffixRows = this.LoadServerExcel<MonsterAffixRow>("MonsterAffixData");
		_resourceManager.ServerDropTreeRows = this.LoadServerExcel<DropTreeRow>("DropTreeData");
		_resourceManager.ServerDropLeafRows = this.LoadServerExcel<DropLeafRow>("DropLeafData");
		_resourceManager.ServerDropSubfieldRows = this.LoadServerExcel<DropSubfieldRow>("DropSubfieldData");
		_resourceManager.ServerEntityDropSubfieldRows = this.LoadServerExcel<EntityDropSubfieldRow>("EntityDropSubfieldData");
		_resourceManager.ServerMonsterDropRows = this.LoadServerExcel<MonsterDropRow>("MonsterDropData");
		_resourceManager.ServerGadgetRows = // Load (GadgetData_Avatar | GadgetData_Equip | GadgetData_Level | GadgetData_Monster | GadgetData_Quest) all as one List<GadgetRow>
			this.LoadServerExcelCombined<GadgetRow>(new string[] {
				"GadgetData_Avatar",
				"GadgetData_Equip",
				"GadgetData_Level",
				"GadgetData_Monster",
				"GadgetData_Quest"
			});

		// ConfigAbilityHashMap is built after ConfigAbility initialization in ResourceManager.
	}



	static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings
	{
		// Strongly typed ability/talent base classes are resolved contextually
		// by AbilityPolymorphicConverter. TypeNameHandling remains enabled only
		// as a compatibility path for legacy object-typed fields carrying $type.
		TypeNameHandling = TypeNameHandling.Objects,
		MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
		SerializationBinder = new AbilitySerializationBinder(),
		Converters = { new AbilityPolymorphicConverter() }
	});

	public List<T> LoadServerExcel<T>(string excelName, TsvParserOptions? options = null) where T : class, new()
	{
		string excelPath = Path.Combine(_baseResourcePath, ServerExcelSubPath, $"{excelName}.txt");
		return TsvReader.ReadFile<T>(excelPath, options);
	}

	public List<T> LoadServerExcelCombined<T>(string[] excelNames, TsvParserOptions? options = null) where T : class, new()
	{
		List<T> combinedList = new List<T>();
		foreach (string excelName in excelNames)
			combinedList.AddRange(LoadServerExcel<T>(excelName, options));
		return combinedList;
	}

}
