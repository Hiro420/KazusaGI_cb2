using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class AddTalentExtraLevel : BaseConfigTalent
{
    [JsonProperty] public readonly string talentType;
    [JsonProperty] public readonly int talentIndex;
    [JsonProperty] public readonly int extraLevel;
}
