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
	private readonly MonsterEntity monster;
	
	public override Dictionary<string, Dictionary<string, float>?>? AbilitySpecials { get; }
	public override HashSet<string> ActiveDynamicAbilities { get; }
	public override Dictionary<string, HashSet<string>> UnlockedTalentParams { get; }
	public override Dictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; }

	public MonsterAbilityManager(MonsterEntity monster) : base(monster)
	{
		this.monster = monster;
		AbilitySpecials = new Dictionary<string, Dictionary<string, float>?>();
		ActiveDynamicAbilities = new HashSet<string>();
		UnlockedTalentParams = new Dictionary<string, HashSet<string>>();
		ConfigAbilityHashMap = new Dictionary<uint, ConfigAbility>();
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