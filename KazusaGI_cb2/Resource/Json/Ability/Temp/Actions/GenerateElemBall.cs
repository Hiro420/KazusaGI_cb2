using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class GenerateElemBall : BaseAction
    {
        [JsonProperty] public readonly int configID;
        [JsonProperty] public readonly BaseBornType born;
        [JsonProperty] public readonly object ratio;
        [JsonProperty] public readonly string baseEnergy;
    }
}
