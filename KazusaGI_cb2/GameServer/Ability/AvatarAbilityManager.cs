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

public class AvatarAbilityManager : BaseAbilityManager
{
	private AvatarEntity _avatar;
	private AvatarSkillDepotExcelConfig CurDepot => MainApp.resourceManager.AvatarSkillDepotExcel[(Owner as AvatarEntity).DbInfo.SkillDepotId];
	private int CurDepotId => (int)(Owner as AvatarEntity).DbInfo.SkillDepotId;
	protected override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap => CurDepot.Abilities;
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => CurDepot.AbilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => [];//CurDepot.ActiveDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => CurDepot.UnlockedTalentParams;


	public AvatarAbilityManager(AvatarEntity avatar) : base(avatar)
	{
		 _avatar = avatar;
	}

	public override void Initialize()
	{
		//foreach (var proudSkill in CurDepot.inherentProudSkillOpens)
		//{
		//	if (proudSkill.openConfig == null || proudSkill.openConfig == "") continue;
		//	foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[(int)(Owner as AvatarEntity).DbInfo.SkillDepotId][proudSkill.openConfig])
		//	{
		//		config.Apply(this, proudSkill.paramList);
		//	}
		//}
		foreach (var skill in CurDepot.GetSkills(_avatar.DbInfo))
		{
			AvatarSkillExcelConfig skillData = (Owner as AvatarEntity).DbInfo.SkillData[(int)CurDepotId][(int)skill.Key];
			ProudSkillExcelConfig proudSkill = (Owner as AvatarEntity).DbInfo.ProudSkillData[CurDepotId];
			if ((Owner as AvatarEntity).DbInfo.ConfigTalentMap.ContainsKey(CurDepotId))
			{
				foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId][proudSkill.openConfig])
				{
					config.Apply(this, proudSkill.paramList);
				}
			}
		}
		if (CurDepot.Element != null)
		{
			int energySkill = (int)CurDepot.energySkill;
			int energySkillLevel = 1; // ???
			AvatarSkillExcelConfig skillData = (Owner as AvatarEntity).DbInfo.SkillData[CurDepotId][energySkill];
			ProudSkillExcelConfig proudSkill = (Owner as AvatarEntity).DbInfo.ProudSkillData[CurDepotId];
			foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[CurDepotId][proudSkill.openConfig])
			{
				config.Apply(this, proudSkill.paramList);
			}
		}
		base.Initialize();
	}
	protected override void AddAbility(AbilityAppliedAbility ability)
	{
		base.AddAbility(ability);
	}

}