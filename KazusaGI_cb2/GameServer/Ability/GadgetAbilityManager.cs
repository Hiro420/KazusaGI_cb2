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
		// Initialize gadget-specific ability behavior
		base.Initialize();
	}

}