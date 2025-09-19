using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    internal class ConfigBornByGlobalValue : BaseBornType
    {
        [JsonProperty] public readonly string positionKey;
        [JsonProperty] public readonly string directionKey;
    }
}
