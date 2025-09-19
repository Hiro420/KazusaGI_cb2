using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.SelectTargetType
{
    internal class SelectTargetsByEquipParts : BaseSelectTargetType
    {
        [JsonProperty] public readonly string[] equipPartNames;
    }
}
