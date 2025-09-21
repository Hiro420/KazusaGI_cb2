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

public class MonsterAbilityManager : BaseAbilityManager
{
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials => throw new NotImplementedException();

	public override HashSet<string> ActiveDynamicAbilities => throw new NotImplementedException();

	public override Dictionary<string, HashSet<string>> UnlockedTalentParams => throw new NotImplementedException();

	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap => throw new NotImplementedException();

	public MonsterAbilityManager(MonsterEntity owner) : base(owner)
	{
	}

	public override Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		return Task.CompletedTask;
	}

	public override void Initialize()
	{
	}

}