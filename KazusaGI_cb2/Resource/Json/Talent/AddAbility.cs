using Newtonsoft.Json;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.Systems.Ability;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class AddAbility : BaseConfigTalent
{
    [JsonProperty] public readonly string abilityName;

    public override void Apply(BaseAbilityManager abilityManager, double[] paramList)
    {
        abilityManager.ActiveDynamicAbilities.Add(abilityName);
    }
}
