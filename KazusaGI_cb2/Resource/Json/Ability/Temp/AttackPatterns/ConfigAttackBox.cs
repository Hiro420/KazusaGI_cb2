using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AttackPatterns
{
    internal class ConfigAttackBox : BaseAttackPattern
    {
        [JsonProperty] public readonly bool filterByFrame;
        [JsonProperty] public readonly Size size;

        public class Size
        {
            [JsonProperty] public readonly float x;
            [JsonProperty] public readonly float y;
            [JsonProperty] public readonly float z;
        }
    }
}
