using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class AttachAbilityStateResistance : BaseAction
    {
        [JsonProperty] public readonly int resistanceListID;
    }
}
