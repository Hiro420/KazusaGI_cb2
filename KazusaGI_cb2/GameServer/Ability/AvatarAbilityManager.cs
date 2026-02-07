using System;
using System.Collections.Generic;
using System.Linq;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Avatar;
using KazusaGI_cb2.Resource.Json.Talent;

namespace KazusaGI_cb2.GameServer.Systems.Ability;

public class AvatarAbilityManager : BaseAbilityManager
{
	private readonly AvatarEntity _avatar;
	private SkillDepot CurDepot => _avatar.DbInfo.CurrentSkillDepot;
	private int CurDepotId => CurDepot.DepotId;
	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap => CurDepot.Abilities;
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => CurDepot.AbilitySpecials;
	public override HashSet<string> ActiveDynamicAbilities => CurDepot.ActiveDynamicAbilities;
	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => CurDepot.UnlockedTalentParams;

	public AvatarAbilityManager(AvatarEntity avatar) : base(avatar)
	{
		_avatar = avatar;
		InitAbilities();
	}

	private void InitAbilities()
	{
		var resourceManager = MainApp.resourceManager;
		var abilityNames = new HashSet<string>(StringComparer.Ordinal);

		var defaultAbilities = resourceManager.GlobalCombatData?.defaultAbilities?.defaultAvatarAbilities;
		if (defaultAbilities != null)
		{
			foreach (var abilityName in defaultAbilities)
			{
				if (!string.IsNullOrWhiteSpace(abilityName))
					abilityNames.Add(abilityName);
			}
		}

		var combatConfig = _avatar.DbInfo.serverAvatarExcel.CombatConfig;
		if (!string.IsNullOrWhiteSpace(combatConfig) &&
			resourceManager.ConfigAvatarMap.TryGetValue(combatConfig, out ConfigAvatar? configAvatar))
		{
			foreach (var entry in configAvatar.abilities)
			{
				var resolved = ResolveAbilityName(entry, resourceManager.ConfigAbilityMap);
				if (!string.IsNullOrWhiteSpace(resolved))
					abilityNames.Add(resolved);
			}
		}

		foreach (var abilityName in abilityNames)
			AddStaticAbility(abilityName);

		foreach (var ability in CurDepot.Abilities.Values)
		{
			if (ability != null)
				AddAbilityToEntity(Owner, ability);
		}
	}

	private void AddStaticAbility(string abilityName)
	{
		var resourceManager = MainApp.resourceManager;
		if (!resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) ||
			container?.Default is not ConfigAbility configAbility)
		{
			return;
		}

		uint hash = KazusaGI_cb2.GameServer.Ability.Utils.AbilityHash(abilityName);
		CurDepot.Abilities[hash] = configAbility;
		if (!CurDepot.AbilitySpecials.ContainsKey(configAbility.abilityName))
			CurDepot.AbilitySpecials[configAbility.abilityName] = BuildAbilitySpecials(configAbility);
	}

	private static string? ResolveAbilityName(TargetAbility entry, Dictionary<string, ConfigAbilityContainer> configMap)
	{
		if (!string.IsNullOrWhiteSpace(entry.abilityOverride) && configMap.ContainsKey(entry.abilityOverride))
			return entry.abilityOverride;
		if (!string.IsNullOrWhiteSpace(entry.abilityName))
			return entry.abilityName;
		return null;
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

	public override void Initialize()
	{
		var avatar = Owner as AvatarEntity;
		if (avatar?.DbInfo == null)
		{
			base.Initialize();
			return;
		}

		var db = avatar.DbInfo;

		if (CurDepot?.InherentProudSkillOpens != null)
		{
			foreach (var proudSkillOpen in CurDepot.InherentProudSkillOpens)
			{
				var openConfig = proudSkillOpen.openConfig;
				if (string.IsNullOrWhiteSpace(openConfig))
					continue;
				if (!db.ConfigTalentMap.TryGetValue(CurDepotId, out var depotTalentMap))
					continue;
				if (!depotTalentMap.TryGetValue(openConfig, out var talents) || talents == null)
					continue;
				foreach (BaseConfigTalent config in talents)
					config?.Apply(this, proudSkillOpen.paramList);
			}
		}

		ProudSkillExcelConfig? FindProudSkill(uint depotId, uint proudSkillGroupId, int level)
		{
			if (!db.ProudSkillData.TryGetValue((int)depotId, out var proudMap) || proudMap == null)
				return null;
			return proudMap.Values.FirstOrDefault(p =>
				p != null &&
				p.proudSkillGroupId == proudSkillGroupId &&
				p.level == level);
		}

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

		if (CurDepot?.Skills != null)
		{
			foreach (var skill in CurDepot.Skills)
			{
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

	public override Protocol.AbilitySyncStateInfo BuildAbilitySyncStateInfo()
	{
		var syncInfo = new Protocol.AbilitySyncStateInfo
		{
			IsInited = false  // Avatars send applied_abilities via AbilityMetaAddAbility, not from config
		};

		if (InstancedModifierMap.Count > 0)
		{
			foreach (var kvp in InstancedModifierMap)
			{
				var modifierController = kvp.Value;
				if (modifierController == null)
					continue;

				var appliedModifier = new Protocol.AbilityAppliedModifier
				{
					ModifierLocalId = modifierController.modifierLocalId,
					ParentAbilityEntityId = modifierController.parentAbilityEntityId,
					ParentAbilityName = new Protocol.AbilityString
					{
						Hash = GameServer.Ability.Utils.AbilityHash(modifierController.parentAbilityName)
					},
					InstancedAbilityId = modifierController.instancedAbilityId,
					InstancedModifierId = modifierController.instancedModifierId,
					ExistDuration = modifierController.existDuration,
					ApplyEntityId = modifierController.applyEntityId,
					IsAttachedParentAbility = modifierController.isAttachedParentAbility
				};

				if (!string.IsNullOrWhiteSpace(modifierController.parentAbilityOverride))
				{
					appliedModifier.ParentAbilityOverride = new Protocol.AbilityString
					{
						Hash = GameServer.Ability.Utils.AbilityHash(modifierController.parentAbilityOverride)
					};
				}

				syncInfo.AppliedModifiers.Add(appliedModifier);
			}
		}

		// DynamicValueMaps: from special overrides and global values
		var abilitySpecialOverrideMap = AbilitySpecialOverrideMap;
		var globalValueHashMap = GlobalValueHashMap;

		if (abilitySpecialOverrideMap.Count > 0)
		{
			foreach (var kvp in abilitySpecialOverrideMap)
			{
				foreach (var specialKvp in kvp.Value)
				{
					var entry = new Protocol.AbilityScalarValueEntry
					{
						Key = new Protocol.AbilityString { Hash = specialKvp.Key },
						ValueType = AbilityScalarType.AbilityScalarTypeFloat,
						FloatValue = specialKvp.Value
					};
					syncInfo.DynamicValueMaps.Add(entry);
				}
			}
		}

		if (globalValueHashMap.Count > 0)
		{
			foreach (var kvp in globalValueHashMap)
			{
				var entry = new Protocol.AbilityScalarValueEntry
				{
					Key = new Protocol.AbilityString { Hash = kvp.Key },
					ValueType = AbilityScalarType.AbilityScalarTypeFloat,
					FloatValue = kvp.Value
				};
				syncInfo.DynamicValueMaps.Add(entry);
			}
		}

		return syncInfo;
	}
}