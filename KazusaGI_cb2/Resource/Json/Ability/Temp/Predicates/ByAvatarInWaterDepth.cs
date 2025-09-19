using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByAvatarInWaterDepth : BasePredicate
    {
        [JsonProperty] public readonly string compareType;
        [JsonProperty] public readonly float depth;
    }
}
