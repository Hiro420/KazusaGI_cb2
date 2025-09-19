using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Talent;

internal class UnlockControllerConditions : BaseConfigTalent
{
    [JsonProperty] public readonly string conditionName;
}
