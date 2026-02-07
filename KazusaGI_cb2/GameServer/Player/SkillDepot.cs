using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.GameServer.Systems.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Talent;
using KazusaGI_cb2.Resource.Excel;
using System.Collections.Generic;
using System.Linq;

namespace KazusaGI_cb2.GameServer
{
    public class SkillDepot
    {
        public int DepotId { get; private set; }
        private PlayerAvatar Owner { get; set; }
        public int EnergySkill { get; private set; }
        public int EnergySkillLevel { get; private set; }
        public ElementType? Element { get; private set; }
        public SortedDictionary<uint, ConfigAbility> Abilities { get; private set; } = new();
        public SortedList<int, int> Skills { get; private set; } = new();
        public HashSet<ProudSkillExcelConfig> InherentProudSkillOpens { get; private set; } = new();
        public HashSet<uint> Talents { get; private set; } = new();
        public Dictionary<string, HashSet<string>> UnlockedTalentParams { get; private set; } = new();
        public HashSet<string> ActiveDynamicAbilities { get; private set; } = new();
        public Dictionary<string, Dictionary<string, float>?> AbilitySpecials { get; private set; } = new();

        public SkillDepot(PlayerAvatar owner, int depotId)
        {
            Owner = owner;
            DepotId = depotId;

            var depotConfig = MainApp.resourceManager.AvatarSkillDepotExcel[(uint)depotId];

            // Initialize energy skill and element type based on skill config.
            EnergySkill = (int)depotConfig.energySkill;
            EnergySkillLevel = Owner.SkillLevels.TryGetValue((uint)EnergySkill, out var energyLevel)
                ? (int)energyLevel
                : 1;
            if (EnergySkill != 0 && MainApp.resourceManager.AvatarSkillExcel.TryGetValue((uint)EnergySkill, out var skillConfig))
            {
                switch (skillConfig.costElemType)
                {
					case KazusaGI_cb2.Resource.ElementType.Fire:
						Element = new Fire();
						break;
					case KazusaGI_cb2.Resource.ElementType.Water:
						Element = new Water();
						break;
					case KazusaGI_cb2.Resource.ElementType.Wind:
						Element = new Wind();
						break;
					case KazusaGI_cb2.Resource.ElementType.Ice:
						Element = new Ice();
						break;
					case KazusaGI_cb2.Resource.ElementType.Rock:
						Element = new Rock();
						break;
					case KazusaGI_cb2.Resource.ElementType.Electric:
						Element = new Electric();
						break;
					case KazusaGI_cb2.Resource.ElementType.Grass:
                        Element = new Grass();
                        break;
				}

                if (Element != null)
                    Element.MaxEnergy = skillConfig.costElemVal;
            }

            // Initialize skills
            foreach (var skillId in depotConfig.skills)
            {
                if (skillId == 0)
                    continue;
                var level = Owner.SkillLevels.TryGetValue(skillId, out var skillLevel) ? (int)skillLevel : 1;
                Skills[(int)skillId] = level;
            }

            // Initialize inherent proud skills
            foreach (var proudSkill in depotConfig.inherentProudSkillOpens)
            {
                if (!MainApp.resourceManager.ProudSkillExcel.TryGetValue(proudSkill.proudSkillGroupId, out var proudSkillConfig))
                    continue;
                InherentProudSkillOpens.Add(proudSkillConfig);
            }

            InitializeConfig();
        }

        private void InitializeConfig()
        {
            Abilities.Clear();
            AbilitySpecials.Clear();

            // Load abilities from the avatar's ability config map
            if (Owner.AbilityHashMap.TryGetValue(DepotId, out var hashMap) && hashMap != null)
            {
                foreach (var kvp in hashMap)
                {
                    Abilities[kvp.Key] = kvp.Value;
                }
            }

            if (Owner.AbilityConfigMap.TryGetValue(DepotId, out var configContainers))
            {
                foreach (var configContainer in configContainers)
                {
                    if (configContainer?.Default is not ConfigAbility config)
                        continue;

                    AbilitySpecials[config.abilityName] = BuildAbilitySpecials(config);
                }
            }

            // Add default avatar abilities like hk4e's AvatarSkillDepotExcelConfig.InitializeConfig.
            var defaultAbilities = MainApp.resourceManager.GlobalCombatData?.defaultAbilities?.defaultAvatarAbilities;
            if (defaultAbilities != null)
            {
                foreach (var abilityName in defaultAbilities)
                {
                    if (string.IsNullOrWhiteSpace(abilityName))
                        continue;
                    if (!MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out var container) ||
                        container?.Default is not ConfigAbility config)
                        continue;

                    uint hash = GameServer.Ability.Utils.AbilityHash(abilityName);
                    Abilities[hash] = config;
                    if (!AbilitySpecials.ContainsKey(config.abilityName))
                    {
                        AbilitySpecials[config.abilityName] = BuildAbilitySpecials(config);
                    }
                }
            }
        }

        private static Dictionary<string, float> BuildAbilitySpecials(ConfigAbility config)
        {
            var specials = new Dictionary<string, float>();
            if (config.abilitySpecials == null)
                return specials;

            foreach (var kvp in config.abilitySpecials)
            {
                if (TryReadSpecialValue(kvp.Value, out var value))
                    specials[kvp.Key] = value;
            }
            return specials;
        }

        private static bool TryReadSpecialValue(object? valueObj, out float value)
        {
            switch (valueObj)
            {
                case null:
                    value = 0f;
                    return false;
                case float floatValue:
                    value = floatValue;
                    return true;
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue:
                    value = longValue;
                    return true;
                case double doubleValue:
                    value = (float)doubleValue;
                    return true;
                case decimal decimalValue:
                    value = (float)decimalValue;
                    return true;
                case string stringValue:
                    return float.TryParse(stringValue, out value);
                default:
                    value = 0f;
                    return false;
            }
        }

        public Dictionary<int, int> GetSkillLevelMap()
        {
            var skillMap = new Dictionary<int, int>();

            if (EnergySkill != 0)
            {
                skillMap[EnergySkill] = EnergySkillLevel;
            }

            foreach (var skill in Skills)
            {
                skillMap[skill.Key] = skill.Value;
            }

            return skillMap;
        }

        public uint GetCoreProudSkillLevel()
        {
            return (uint)Talents.Count;
        }

        public void AddTalent(uint talentId, BaseAbilityManager abilityManager)
        {
            Talents.Add(talentId);

            // Apply talent configuration if available
            var talentData = Owner.TalentData[DepotId][talentId];
			Talents.Add(talentData.talentId);
			foreach (BaseConfigTalent config in Owner.ConfigTalentMap[DepotId][talentData.openConfig])
			{
				config.Apply(abilityManager, talentData.paramList.ToArray());
			}
		}
    }
}