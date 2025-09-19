using Newtonsoft.Json;
using KazusaGI_cb2.GameServer.Ability;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class ModifySkillCD : BaseConfigTalent
{
    [JsonProperty] public readonly int overtime;
    [JsonProperty] public readonly float cdRatio;

    public override void Apply(BaseAbilityManager abilityManager, double[] paramList)
    {
        //TODO
    }
}
