using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class EnableRocketJump : BaseAction
    {
        [JsonProperty] public readonly bool canBeHandledOnRecover;
        [JsonProperty] public readonly bool enable;
        [JsonProperty] public readonly Extension extention;

        public class Extension
        {
            [JsonProperty] public readonly float xzMultiplier;
            [JsonProperty] public readonly float yMultiplier;
        }
    }
}
