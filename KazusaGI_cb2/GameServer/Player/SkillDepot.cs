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
        public Dictionary<uint, ConfigAbility> Abilities { get; private set; } = new();
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
            
            // Initialize energy skill
            EnergySkill = (int)depotConfig.energySkill;
            EnergySkillLevel = 1;
            
            // Initialize element type based on energy skill
            if (EnergySkill != 0 && MainApp.resourceManager.AvatarSkillExcel.ContainsKey((uint)EnergySkill))
            {
                var skillConfig = MainApp.resourceManager.AvatarSkillExcel[(uint)EnergySkill];
                // Initialize element based on skill config
                Element = new ElementType(); // You may need to set specific type based on skillConfig
            }

            // Initialize skills
            foreach (var skillId in depotConfig.skills)
            {
                if (skillId != 0) 
                {
                    Skills.Add((int)skillId, 1);
                }
            }

            // Initialize inherent proud skills
            foreach (var proudSkill in depotConfig.inherentProudSkillOpens)
            {
                if (MainApp.resourceManager.ProudSkillExcel.ContainsKey(proudSkill.proudSkillGroupId))
                {
                    var proudSkillConfig = MainApp.resourceManager.ProudSkillExcel[proudSkill.proudSkillGroupId];
                    InherentProudSkillOpens.Add(proudSkillConfig);
                }
            }

            InitializeConfig();
        }

        private void InitializeConfig()
        {
            Abilities.Clear();
            AbilitySpecials.Clear();
            
            // Load abilities from the avatar's ability config map
            if (Owner.AbilityHashMap.ContainsKey(DepotId))
            {
                var hashMap = Owner.AbilityHashMap[DepotId];
                if (hashMap != null)
                {
                    foreach (var kvp in hashMap)
                    {
                        Abilities[kvp.Key] = kvp.Value;
                    }
                }
            }

            // Load ability specials from ability config map
            if (Owner.AbilityConfigMap.ContainsKey(DepotId))
            {
                foreach (var configContainer in Owner.AbilityConfigMap[DepotId])
                {
                    if (configContainer?.Default is ConfigAbility config)
                    {
                        AbilitySpecials[config.abilityName] = config.abilitySpecials;
                    }
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
                }
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