using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.SelectTargetType
{
    internal class SelectTargetsByChildren : BaseSelectTargetType
    {
        [JsonProperty] public readonly int configID;
        [JsonProperty] public readonly string sortType;
    }
}
