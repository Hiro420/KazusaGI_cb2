using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class SetSurroundAnchor : BaseAction
    {
        [JsonProperty("anchorName")]
        public string AnchorName { get; set; } = string.Empty;

        [JsonProperty("surroundRadius")]
        public float SurroundRadius { get; set; }

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}