using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.Systems.Ability;
using Newtonsoft.Json;

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
