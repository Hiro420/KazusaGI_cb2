using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace KazusaGI_cb2.Resource.Excel;

public class AvatarSkillDepotExcelConfig
{
	private static Logger logger = new Logger("AvatarSkillDepot");
	public uint id;
    public uint energySkill;
    public List<uint> skills;
    public List<uint> subSkills;
    public uint attackModeSkill;
    public uint leaderTalent;
    public List<string> extraAbilities;
    public List<uint> talents;
    public string talentStarName;
    public uint coreProudSkillGroupId;
    public uint coreProudAvatarPromoteLevel;

    public List<ProudSkillOpenConfig> inherentProudSkillOpens = new();
	public Dictionary<uint, ConfigAbility>? Abilities;
	public Dictionary<string, Dictionary<string, float>?>? AbilitySpecials = new();
	public SortedList<int, int> ProudSkillExtraLevelMap = new();
	public Dictionary<string, HashSet<string>> UnlockedTalentParams = new();

	public GameServer.Ability.ElementType? Element = null;

	public void InitializeConfig(PlayerAvatar data)
	{
		var EnergySkill = data.avatarSkillDepotExcel.energySkill;
		if (EnergySkill != 0)
		{
			ElementType type = data.SkillData[(int)data.SkillDepotId][EnergySkill].costElemType;
			switch (type)
			{
				case ElementType.Fire:
					Element = new Fire();
					break;
				case ElementType.Water:
					Element = new Water();
					break;
				case ElementType.Wind:
					Element = new Wind();
					break;
				case ElementType.Ice:
					Element = new Ice();
					break;
				case ElementType.Rock:
					Element = new Rock();
					break;
				case ElementType.Electric:
					Element = new Electric();
					break;
				case ElementType.Grass:
					Element = new Grass();
					break;
				default:
					logger.LogWarning("Unknown Avatar Element Type");
					break;
			}
			Element.MaxEnergy = data.SkillData[(int)data.SkillDepotId][EnergySkill].costElemVal;
		}

		var DepotId = (int)data.SkillDepotId;

		Abilities = new();
		AbilitySpecials = new();
		ProudSkillExtraLevelMap = new();
		foreach (var configAbility in data.AbilityConfigMap[DepotId])
		{
			if (configAbility.Default is ConfigAbility config)
				AbilitySpecials.Add(config.abilityName, config.abilitySpecials);
		}
		if (data.AbilityHashMap.TryGetValue(DepotId, out Dictionary<uint, ConfigAbility>? hashMap))
			Abilities = hashMap;

		foreach (string abilityName in MainApp.resourceManager.GlobalCombatData.defaultAbilities.defaultAvatarAbilities)
		{
			var container_common = MainApp.resourceManager.ConfigAbilityMap["ConfigAbility_Avatar_Common"];
			{
				if (container_common.Default is ConfigAbility config_common)
				{
					Abilities[(uint)GameServer.Ability.Utils.AbilityHash(config_common.abilityName)] = config_common;
				}
			}
		}

		var inherentProudSkillGroups = data.avatarSkillDepotExcel.inherentProudSkillOpens.Where(w => w.needAvatarPromoteLevel <= 1).ToDictionary(q => q.proudSkillGroupId);
		foreach (var group in inherentProudSkillGroups.Values)
		{
			inherentProudSkillOpens.Add(group!);
		}
	}

	public Dictionary<uint, ConfigAbility>? GetSkills(PlayerAvatar data)
    {
		Dictionary<uint, ConfigAbility> Abilities = new();

		if (data.AbilityHashMap.TryGetValue((int)this.id, out Dictionary<uint, ConfigAbility>? hashMap))
			Abilities = hashMap;

		foreach (string abilityName in MainApp.resourceManager.GlobalCombatData.defaultAbilities.defaultAvatarAbilities)
		{
			var container = MainApp.resourceManager.ConfigAbilityMap[abilityName];
			if (container.Default is ConfigAbility configdefault)
			{
				Abilities[(uint)GameServer.Ability.Utils.AbilityHash(configdefault.abilityName)] = configdefault;
			}
		}

		return Abilities;
	}
}