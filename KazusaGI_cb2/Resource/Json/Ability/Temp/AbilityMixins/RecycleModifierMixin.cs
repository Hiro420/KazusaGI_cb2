using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    public class RecycleModifierMixin : BaseAbilityMixin
    {
        [JsonProperty("recycleInterval")]
        public float RecycleInterval { get; set; }

        [JsonProperty("maxRecycleCount")]
        public int MaxRecycleCount { get; set; }

        [JsonProperty("recycleCondition")]
        public object RecycleCondition { get; set; } = new object();
    }
}