using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    internal class ByCurrentSceneId : BasePredicate
    {
        [JsonProperty] public readonly int[] sceneIds;
    }
}
