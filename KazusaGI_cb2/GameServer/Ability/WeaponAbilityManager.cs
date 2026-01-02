using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.GameServer.Systems.Ability;
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

public class WeaponAbilityManager : BaseAbilityManager
{
	private WeaponEntity _weapon => (WeaponEntity)Owner;

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = new();

	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => new();

	public override HashSet<string> ActiveDynamicAbilities => new();

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => new();

	public WeaponAbilityManager(Entity owner) : base(owner)
	{
    }

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// Use the base implementation for gadget ability handling
		await base.HandleAbilityInvokeAsync(invoke);
	}

	public override void Initialize()
	{
		// Initialize weapon-specific ability behavior

		HashSet<string> abilityNames = new();
		var resourceManager = MainApp.resourceManager;

		foreach (uint affixId in _weapon.GetAffixMap().Keys)
		{
			if (affixId == 0) continue;
			List<EquipAffixExcelConfig>? affixConfigs = resourceManager.EquipAffixExcel.Values.Where(e => e.AffixId == affixId).ToList();
			foreach (var affixConfig in affixConfigs)
			{
				if (string.IsNullOrWhiteSpace(affixConfig.OpenConfig))
					continue;
				abilityNames.Add(affixConfig.OpenConfig!);
			}
		}

		foreach (string abilityName in abilityNames)
		{
			if (!string.IsNullOrWhiteSpace(abilityName))
			{
				var abilityData = MainApp.resourceManager.ConfigAbilityMap[abilityName];
				if (abilityData != null)
				{
					var config = (ConfigAbility)abilityData.Default!;
					ConfigAbilityHashMap[Utils.AbilityHash(abilityName)] = config;
					AddAbilityToEntity(Owner, config);
				}
			}
		}

		base.Initialize();
	}

}