using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Predicates
{
    public class BySceneSurfaceType : BasePredicate
    {
        [JsonProperty("surfaceType")]
        public string SurfaceType { get; set; } = string.Empty;

        [JsonProperty("checkRadius")]
        public float CheckRadius { get; set; } = 1.0f;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}