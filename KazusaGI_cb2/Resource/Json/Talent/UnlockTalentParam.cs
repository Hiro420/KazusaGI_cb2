using KazusaGI_cb2.GameServer.Systems.Ability;
using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class UnlockTalentParam : BaseConfigTalent
{
	[JsonProperty] public readonly string abilityName;
	[JsonProperty] public readonly string talentParam;

	public override void Apply(BaseAbilityManager abilityManager, double[] paramList)
	{
		if (abilityManager.UnlockedTalentParams.ContainsKey(abilityName))
			abilityManager.UnlockedTalentParams[abilityName].Add(talentParam);
		else
		{
			abilityManager.UnlockedTalentParams[abilityName] = new() { talentParam };
		}
	}
}
