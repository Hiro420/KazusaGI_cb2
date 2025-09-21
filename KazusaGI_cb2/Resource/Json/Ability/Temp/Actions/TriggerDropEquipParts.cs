using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class TriggerDropEquipParts : BaseAction
    {
        [JsonProperty] public readonly string[] equipPartNames;
        [JsonProperty] public readonly bool useGrassDisplacement;
    }
}