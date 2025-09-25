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

public class GadgetAbilityManager : BaseAbilityManager
{
	private readonly GadgetEntity gadget;
	
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => gadget.AbilitySpecials;
	public override HashSet<string> ActiveDynamicAbilities => gadget.ActiveDynamicAbilities;
	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => gadget.UnlockedTalentParams;
	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap => gadget.AbilityHashMap;

	public GadgetAbilityManager(GadgetEntity gadget) : base(gadget)
	{
		this.gadget = gadget;
	}

	public override async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		await base.HandleAbilityInvokeAsync(invoke);
	}

	public override void Initialize()
	{
		base.Initialize();
	}
}