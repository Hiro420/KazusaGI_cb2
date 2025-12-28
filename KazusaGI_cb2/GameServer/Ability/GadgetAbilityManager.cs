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

public class GadgetAbilityManager : BaseAbilityManager
{
	private GadgetEntity _gadget => (GadgetEntity)Owner;
	
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => _gadget.AbilitySpecials;

	public override HashSet<string> ActiveDynamicAbilities => _gadget.ActiveDynamicAbilities;

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => _gadget.UnlockedTalentParams;

	public override SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap => new(_gadget.AbilityHashMap);

	public GadgetAbilityManager(Entity owner) : base(owner)
	{
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		// Use the base implementation for gadget ability handling
		await base.HandleAbilityInvokeAsync(invoke);
	}

	public override void Initialize()
	{
		List<string> abilityNames = new();

		uint gadgetId = _gadget._gadgetId;

		var resourceManager = MainApp.resourceManager;
		if (resourceManager.ConfigPreload != null &&
			resourceManager.ConfigPreload.entitiesPreload != null &&
			resourceManager.ConfigPreload.entitiesPreload.TryGetValue(gadgetId, out var preloadInfo))
		{
			foreach (var fullPath in preloadInfo.abilities.onCreate)
			{
				if (resourceManager.AbilityPathData != null &&
					resourceManager.AbilityPathData.abilityPaths.TryGetValue(fullPath, out var pathData))
				{
					foreach (var abilityName in pathData)
					{
						if (!string.IsNullOrWhiteSpace(abilityName))
							abilityNames.Add(abilityName);
					}
				}
			}
		}

		var configAbilityMap = resourceManager.ConfigAbilityMap;
		if (configAbilityMap != null)
		{
			foreach (var abilityName in abilityNames)
			{
				if (!configAbilityMap.TryGetValue(abilityName, out ConfigAbilityContainer? container) ||
					container == null ||
					container.Default == null)
				{
					continue;
				}

				if (container.Default is not ConfigAbility configAbility)
					continue;

				uint hash = GameServer.Ability.Utils.AbilityHash(abilityName);
				ConfigAbilityHashMap[hash] = configAbility;
			}
		}

		base.Initialize();

		// Finally, mirror hk4e's Monster::initAbility by attaching all
		// resolved config abilities to the monster entity's AbilityComp.
		foreach (var kvp in ConfigAbilityHashMap)
		{
			var ability = kvp.Value;
			if (ability != null)
			{
				AddAbilityToEntity(_gadget, ability);
			}
		}
	}

}