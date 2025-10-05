using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Talent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public class AvatarAbilityManager : AbilityManager
{
	private AvatarEntity _avatar;
	private SkillDepot CurDepot => (Owner as AvatarEntity).DbInfo.CurrentSkillDepot;
	private int CurDepotId => CurDepot.DepotId;
	public static Dictionary<uint, ConfigAbility> ConfigAbilityHashMap;
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => CurDepot.AbilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => CurDepot.ActiveDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => CurDepot.UnlockedTalentParams;


	public AvatarAbilityManager(AvatarEntity avatar) : base(avatar)
	{
		 _avatar = avatar;
	}

	public override void Initialize()
	{
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
	protected override void AddAbility(AbilityAppliedAbility ability)
	{
		base.AddAbility(ability);
	}

}