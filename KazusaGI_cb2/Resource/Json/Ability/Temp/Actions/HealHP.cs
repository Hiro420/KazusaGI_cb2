using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class HealHP : BaseAction
    {
        [JsonProperty] public readonly bool doOffStage;
        [JsonProperty] public object? amount;
    }
}
