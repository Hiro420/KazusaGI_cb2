using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    internal class ConfigBornByTargetLinearPoint : BaseBornType
    {
        [JsonProperty] public readonly float linearOffset;
        [JsonProperty] public readonly bool linearXZ;
        [JsonProperty] public readonly bool baseOnTarget;
    }
}
