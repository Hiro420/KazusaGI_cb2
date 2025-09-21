using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class GetPos : BaseAction
    {
        [JsonProperty("positionKey")]
        public string PositionKey { get; set; } = string.Empty;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();

        [JsonProperty("offset")]
        public object Offset { get; set; } = new object();
    }
}