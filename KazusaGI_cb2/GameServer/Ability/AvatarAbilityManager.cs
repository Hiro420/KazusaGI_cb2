using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Talent;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Ability;

namespace KazusaGI_cb2.GameServer.Systems.Ability;

public class AvatarAbilityManager : BaseAbilityManager
{
	private SkillDepot CurDepot => (Owner as AvatarEntity).DbInfo.CurrentSkillDepot;
	private int CurDepotId => CurDepot.DepotId;
	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap => new(CurDepot.Abilities);

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => CurDepot.AbilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => CurDepot.ActiveDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => CurDepot.UnlockedTalentParams;


	public AvatarAbilityManager(AvatarEntity avatar) : base(avatar)
	{
		InitAbilities();
	}

	private void InitAbilities()
	{
		// Initialize scene-specific abilities here if needed
		foreach (string abilityName in MainApp.resourceManager.GlobalCombatData!.defaultAbilities!.defaultAvatarAbilities!)
		{
			if (!string.IsNullOrWhiteSpace(abilityName))
			{
				var abilityData = MainApp.resourceManager.ConfigAbilityMap[abilityName];
				if (abilityData != null)
				{
					ConfigAbilityHashMap[GameServer.Ability.Utils.AbilityHash(abilityName)] = (ConfigAbility)abilityData.Default!;
					AddAbilityToEntity(Owner, (ConfigAbility)abilityData.Default!);
				}
			}
		}
	}

	public override void Initialize()
	{
		foreach (var proudSkill in CurDepot.InherentProudSkillOpens)
		{
			if (proudSkill.openConfig == null || proudSkill.openConfig == "") continue;
			foreach (BaseConfigTalent config in (Owner as AvatarEntity).DbInfo.ConfigTalentMap[(Owner as AvatarEntity).DbInfo.CurrentSkillDepot.DepotId][proudSkill.openConfig])
			{
				config.Apply(this, proudSkill.paramList);
			}
		}
		foreach (var skill in CurDepot.Skills)
		{
			AvatarSkillExcelConfig skillData = (Owner as AvatarEntity).DbInfo.SkillData[CurDepotId][(uint)skill.Key];
			ProudSkillExcelConfig proudSkill = (Owner as AvatarEntity).DbInfo.ProudSkillData[CurDepotId]
					.Where(w => w.Value.proudSkillGroupId == skillData.proudSkillGroupId && w.Value.level == skill.Value).First().Value;
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
			uint energySkill = (uint)CurDepot.EnergySkill;
			int energySkillLevel = CurDepot.EnergySkillLevel;
			AvatarSkillExcelConfig skillData = (Owner as AvatarEntity).DbInfo.SkillData[CurDepotId][energySkill];
			ProudSkillExcelConfig proudSkill = (Owner as AvatarEntity).DbInfo.ProudSkillData[CurDepotId]
					.Where(w => w.Value.proudSkillGroupId == skillData.proudSkillGroupId && w.Value.level == energySkillLevel).First().Value;
			foreach (BaseConfigTalent config in
				(Owner as AvatarEntity).DbInfo.ConfigTalentMap.Values
					.Where(x => x.ContainsKey(proudSkill.openConfig))
					.SelectMany(t => t.Values)          // IEnumerable<BaseConfigTalent[]>
					.SelectMany(arr => arr))           // IEnumerable<BaseConfigTalent>
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
