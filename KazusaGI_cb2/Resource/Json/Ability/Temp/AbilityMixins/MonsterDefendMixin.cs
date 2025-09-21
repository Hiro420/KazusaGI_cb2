using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    public class MonsterDefendMixin : BaseAbilityMixin
    {
        [JsonProperty("defendType")]
        public string DefendType { get; set; } = string.Empty;

        [JsonProperty("defendDirection")]
        public object DefendDirection { get; set; } = new object();

        [JsonProperty("defendAngle")]
        public float DefendAngle { get; set; }
    }
}