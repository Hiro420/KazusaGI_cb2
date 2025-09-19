using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByEntityAppearVisionType : BasePredicate
    {
        [JsonProperty] public readonly VisionType visionType;
    }
}
