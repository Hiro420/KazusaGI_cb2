using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    public class IceFloorMixin : BaseAbilityMixin
    {
        [JsonProperty("iceEffectPattern")]
        public object IceEffectPattern { get; set; } = new object();

        [JsonProperty("iceDuration")]
        public float IceDuration { get; set; }

        [JsonProperty("iceRadius")]
        public float IceRadius { get; set; }
    }
}