using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.DirectionTypes
{
    internal class ConfigDirectionByAttachPoint : BaseDirectionType
    {
        [JsonProperty] public readonly string attachPointName;
        [JsonProperty] public readonly TargetType attachPointTargetType;
    }
}
