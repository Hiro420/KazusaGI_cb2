using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.SelectTargetType
{
    internal class SelectTargetsByShape : BaseSelectTargetType
    {
        [JsonProperty] public readonly string shapeName;
        [JsonProperty] public readonly string centerBasedOn;
        [JsonProperty] public readonly string campTargetType;
        [JsonProperty] public readonly string campBasedOn;
        [JsonProperty] public readonly int topLimit;
        [JsonProperty] public readonly object sizeRatio;
    }
}
