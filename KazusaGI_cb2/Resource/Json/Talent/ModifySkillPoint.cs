using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class ModifySkillPoint : BaseConfigTalent
{
    [JsonProperty] public readonly int overtime;
    [JsonProperty] public readonly int pointDelta;
}
