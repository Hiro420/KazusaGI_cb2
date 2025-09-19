using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetVelocityIgnoreAirGY : BaseAction
    {
        [JsonProperty] public readonly bool? ignoreAirGY;
        [JsonProperty] public readonly bool? doOffStage;
    }
}
