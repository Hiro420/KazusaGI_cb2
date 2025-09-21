using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Talent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public class AvatarAbilityManager : BaseAbilityManager
{
	private AvatarEntity _avatar;
	private SkillDepot CurDepot => (Owner as AvatarEntity).DbInfo.CurrentSkillDepot;
	private int CurDepotId => CurDepot.DepotId;
	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap => CurDepot.Abilities;
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => CurDepot.AbilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => CurDepot.ActiveDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => CurDepot.UnlockedTalentParams;


	public AvatarAbilityManager(AvatarEntity avatar) : base(avatar)
	{
		 _avatar = avatar;
    }

	public override void Initialize()
	{
		// First, initialize abilities for this avatar from ConfigAbilityHashMap
		InitializeAvatarAbilities();
		
		// Initialize inherent proud skills first
		foreach (var proudSkill in CurDepot.InherentProudSkillOpens)
		{
			if (!string.IsNullOrEmpty(proudSkill.openConfig) && 
				(Owner as AvatarEntity).DbInfo.ConfigTalentMap.ContainsKey(CurDepotId) && 
				(Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId].ContainsKey(proudSkill.openConfig))
			{
				foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId][proudSkill.openConfig])
				{
					config.Apply(this, proudSkill.paramList);
				}
			}
		}
		
		// Process skills from the skill depot
		foreach (var skill in CurDepot.Skills)
		{
			int skillId = skill.Key;
			int skillLevel = skill.Value;
			
			if (MainApp.resourceManager.AvatarSkillExcel.ContainsKey((uint)skillId))
			{
				var skillConfig = MainApp.resourceManager.AvatarSkillExcel[(uint)skillId];
				
				// Find the corresponding proud skill
				var proudSkillEntry = MainApp.resourceManager.ProudSkillExcel.Values
					.FirstOrDefault(ps => ps.proudSkillGroupId == skillConfig.proudSkillGroupId && ps.level == skillLevel);
					
				if (proudSkillEntry != null && !string.IsNullOrEmpty(proudSkillEntry.openConfig) &&
					(Owner as AvatarEntity).DbInfo.ConfigTalentMap.ContainsKey(CurDepotId) &&
					(Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId].ContainsKey(proudSkillEntry.openConfig))
				{
					foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId][proudSkillEntry.openConfig])
					{
						config.Apply(this, proudSkillEntry.paramList);
					}
				}
			}
		}
		// Process energy skill
		if (CurDepot.Element != null && CurDepot.EnergySkill != 0)
		{
			int energySkill = CurDepot.EnergySkill;
			int energySkillLevel = CurDepot.EnergySkillLevel;
			
			if (MainApp.resourceManager.AvatarSkillExcel.ContainsKey((uint)energySkill))
			{
				var skillConfig = MainApp.resourceManager.AvatarSkillExcel[(uint)energySkill];
				
				var proudSkillEntry = MainApp.resourceManager.ProudSkillExcel.Values
					.FirstOrDefault(ps => ps.proudSkillGroupId == skillConfig.proudSkillGroupId && ps.level == energySkillLevel);
				
				if (proudSkillEntry != null && !string.IsNullOrEmpty(proudSkillEntry.openConfig) && 
					(Owner as AvatarEntity).DbInfo.ConfigTalentMap.ContainsKey(CurDepotId) &&
					(Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId].ContainsKey(proudSkillEntry.openConfig))
				{
					foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId][proudSkillEntry.openConfig])
					{
						config.Apply(this, proudSkillEntry.paramList);
					}
				}
			}
		}
		base.Initialize();
	}

	/// <summary>
	/// Override the base InitializeEntityAbilities to use avatar-specific initialization
	/// </summary>
	public override void InitializeEntityAbilities(Entity entity)
	{
		// Call the avatar-specific ability initialization
		InitializeAvatarAbilities();
	}

	/// <summary>
	/// Initialize abilities for this avatar by adding them to the entity's InstancedAbilities collection
	/// </summary>
	private void InitializeAvatarAbilities()
	{
		try
		{
			// Clear any existing abilities to avoid duplicates
			Owner.InstancedAbilities.Clear();
			InstanceToAbilityHashMap.Clear();
			
			logger.LogInfo($"Initializing abilities for avatar {_avatar.DbInfo.AvatarName}", false);
			logger.LogInfo($"ConfigAbilityMap contains {MainApp.resourceManager.ConfigAbilityMap.Count} abilities", false);
			
			int abilitiesAdded = 0;
			
			// First, add basic avatar abilities from ConfigAvatarMap
			var avatarConfigName = $"ConfigAvatar_{_avatar.DbInfo.AvatarName}";
			if (MainApp.resourceManager.ConfigAvatarMap.ContainsKey(avatarConfigName))
			{
				var avatarConfig = MainApp.resourceManager.ConfigAvatarMap[avatarConfigName];
				if (avatarConfig.abilities != null)
				{
					foreach (TargetAbility Tability in avatarConfig.abilities)
					{
						if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(Tability.abilityName, out var abilityContainer))
						{
							var configAbility = (ConfigAbility)abilityContainer.Default;
							AddAbilityToEntity(Owner, configAbility);
							abilitiesAdded++;
						}
					}
				}
			}
			
			// Add common avatar abilities that all avatars should have
			var commonAvatarAbilities = new[]
			{
				"Avatar_DefaultAbility_VisionReplaceDieInvincible",
				"Avatar_DefaultAbility_AvartarInShaderChange"
			};
			
			foreach (var abilityName in commonAvatarAbilities)
			{
				if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out var abilityContainer))
				{
					var configAbility = (ConfigAbility)abilityContainer.Default;
					AddAbilityToEntity(Owner, configAbility);
					abilitiesAdded++;
				}
			}
			
			// Add skill-based abilities for the current skill depot
			foreach (var skillEntry in CurDepot.Skills)
			{
				int skillId = skillEntry.Key;
				if (MainApp.resourceManager.AvatarSkillExcel.ContainsKey((uint)skillId))
				{
					var skillConfig = MainApp.resourceManager.AvatarSkillExcel[(uint)skillId];
					
					// Try to find abilities based on skill name or id
					var skillAbilityName = $"Avatar_{_avatar.DbInfo.AvatarName}_Skill_{skillId}";
					if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(skillAbilityName, out var skillAbilityContainer))
					{
						var configAbility = (ConfigAbility)skillAbilityContainer.Default;
						AddAbilityToEntity(Owner, configAbility);
						abilitiesAdded++;
					}
				}
			}
			
			// Add energy skill ability
			if (CurDepot.EnergySkill != 0)
			{
				var energySkillAbilityName = $"Avatar_{_avatar.DbInfo.AvatarName}_Skill_{CurDepot.EnergySkill}";
				if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(energySkillAbilityName, out var energyAbilityContainer))
				{
					var configAbility = (ConfigAbility)energyAbilityContainer.Default;
					AddAbilityToEntity(Owner, configAbility);
					abilitiesAdded++;
				}
			}
			
			// If still no abilities were added, add some fallback abilities
			if (abilitiesAdded == 0)
			{
				logger.LogWarning($"No specific abilities found for avatar {_avatar.DbInfo.AvatarName}, trying generic abilities");
				
				// Try to find any abilities that contain the avatar name
				var avatarAbilities = MainApp.resourceManager.ConfigAbilityMap.Keys
					.Where(name => name.Contains(_avatar.DbInfo.AvatarName, StringComparison.OrdinalIgnoreCase))
					.Take(5)
					.ToList();
				
				foreach (var abilityName in avatarAbilities)
				{
					var abilityContainer = MainApp.resourceManager.ConfigAbilityMap[abilityName];
					var configAbility = (ConfigAbility)abilityContainer.Default;
					AddAbilityToEntity(Owner, configAbility);
					abilitiesAdded++;
				}
			}
			
			logger.LogInfo($"Avatar {_avatar.DbInfo.AvatarName} initialized with {Owner.InstancedAbilities.Count} abilities", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to initialize avatar abilities: {ex.Message}");
		}
	}
	protected override void AddAbility(AbilityAppliedAbility ability)
	{
		base.AddAbility(ability);
	}

}