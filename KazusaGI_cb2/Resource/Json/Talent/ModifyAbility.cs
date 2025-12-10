using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.Systems.Ability;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class ModifyAbility : BaseConfigTalent
{
    [JsonProperty] public readonly string abilityName;
    [JsonProperty] public readonly string paramSpecial;
    [JsonProperty] public readonly object? paramDelta; // plus or minus &(index in proudSkillData's or AvatarTalentData's paramList) or absolute delta
    [JsonProperty] public readonly object? paramRatio; // plus or minus &(index in proudSkillData's or AvatarTalentData's paramList) or absolute delta

    public override void Apply(BaseAbilityManager abilityManager, double[] paramList)
    {
        // Check if AbilitySpecials exists and contains the ability name
        if (abilityManager.AbilitySpecials == null || 
            !abilityManager.AbilitySpecials.ContainsKey(abilityName) ||
            abilityManager.AbilitySpecials[abilityName] == null ||
            !abilityManager.AbilitySpecials[abilityName].ContainsKey(paramSpecial))
        {
            // Skip if the ability or parameter doesn't exist
            return;
        }

        float special = abilityManager.AbilitySpecials[abilityName][paramSpecial];
        //if (paramDelta is string deltaString)
        //{
        //    string index = Regex.Replace(deltaString, "%", "");
        //    float delta = (float)paramList[int.Parse(index)];
        //    special += delta;
        //}
        //else if (paramDelta is double asD)
        //    special += (float)asD;

        //if (paramRatio is string ratioString)
        //{
        //    string index = Regex.Replace(ratioString, "%", "");
        //    float ratio = (float)paramList[int.Parse(index)];
        //    special *= ratio;
        //}
        //else if (paramRatio is double asD)
        //{
        //    if (asD != 0)
        //        special *= (float)asD;
        //}

        abilityManager.AbilitySpecials[abilityName][paramSpecial] = special;
    }
}
