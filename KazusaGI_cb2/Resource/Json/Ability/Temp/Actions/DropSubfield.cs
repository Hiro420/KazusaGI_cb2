using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class DropSubfield : BaseAction
    {
        [JsonProperty] public readonly int subfieldDropCount;
        [JsonProperty] public readonly string dropPredicates;
    }
}