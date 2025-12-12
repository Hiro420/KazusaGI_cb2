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
		var avatar = Owner as AvatarEntity;
		if (avatar?.DbInfo == null)
		{
			base.Initialize();
			return;
		}

		var db = avatar.DbInfo;

		// ---------- Inherent proud skill opens ----------
		if (CurDepot?.InherentProudSkillOpens != null)
		{
			foreach (var proudSkillOpen in CurDepot.InherentProudSkillOpens)
			{
				var openConfig = proudSkillOpen.openConfig;
				if (string.IsNullOrWhiteSpace(openConfig))
					continue;

				// ConfigTalentMap[depotId][openConfig]
				if (!db.ConfigTalentMap.TryGetValue(CurDepotId, out var depotTalentMap))
					continue;

				if (!depotTalentMap.TryGetValue(openConfig, out var talents) || talents == null)
					continue;

				foreach (BaseConfigTalent config in talents)
					config?.Apply(this, proudSkillOpen.paramList);
			}
		}

		// Local helper to find proud skill safely
		ProudSkillExcelConfig? FindProudSkill(uint depotId, uint proudSkillGroupId, int level)
		{
			if (!db.ProudSkillData.TryGetValue((int)depotId, out var proudMap) || proudMap == null)
				return null;

			// proudMap is Dictionary<uint, ProudSkillExcelConfig>
			return proudMap.Values.FirstOrDefault(p =>
				p != null &&
				p.proudSkillGroupId == proudSkillGroupId &&
				p.level == level);
		}

		// Local helper to apply talents from depot map safely
		void ApplyTalentsFromDepot(int depotId, string? openConfig, IList<double>? paramList)
		{
			if (string.IsNullOrWhiteSpace(openConfig))
				return;

			if (!db.ConfigTalentMap.TryGetValue(depotId, out var depotTalentMap) || depotTalentMap == null)
				return;

			if (!depotTalentMap.TryGetValue(openConfig, out var talents) || talents == null)
				return;

			foreach (var config in talents)
				config?.Apply(this, paramList?.ToArray() ?? Array.Empty<double>());
		}

		// ---------- Skills ----------
		if (CurDepot?.Skills != null)
		{
			foreach (var skill in CurDepot.Skills)
			{
				// SkillData[depotId][skillId]
				if (!db.SkillData.TryGetValue(CurDepotId, out var skillMap) || skillMap == null)
					continue;

				var skillId = (uint)skill.Key;
				if (!skillMap.TryGetValue(skillId, out var skillData) || skillData == null)
					continue;

				var proudSkill = FindProudSkill((uint)CurDepotId, skillData.proudSkillGroupId, skill.Value);
				if (proudSkill == null)
					continue;

				ApplyTalentsFromDepot(CurDepotId, proudSkill.openConfig, proudSkill.paramList);
			}
		}

		// ---------- Energy skill (elemental burst) ----------
		if (CurDepot?.Element != null)
		{
			uint energySkillId = (uint)CurDepot.EnergySkill;
			int energySkillLevel = CurDepot.EnergySkillLevel;

			if (db.SkillData.TryGetValue(CurDepotId, out var skillMap) &&
				skillMap != null &&
				skillMap.TryGetValue(energySkillId, out var energySkillData) &&
				energySkillData != null)
			{
				var proudSkill = FindProudSkill((uint)CurDepotId, energySkillData.proudSkillGroupId, energySkillLevel);
				if (proudSkill != null && !string.IsNullOrWhiteSpace(proudSkill.openConfig))
				{
					// Apply across all depots that contain that openConfig
					foreach (var depotTalentMap in db.ConfigTalentMap.Values)
					{
						if (depotTalentMap == null) continue;
						if (!depotTalentMap.TryGetValue(proudSkill.openConfig, out var talents) || talents == null) continue;

						foreach (var config in talents)
							config?.Apply(this, proudSkill.paramList);
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
