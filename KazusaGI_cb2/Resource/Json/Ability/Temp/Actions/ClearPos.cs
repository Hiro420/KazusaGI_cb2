using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class ClearPos : BaseAction
    {
        [JsonProperty("positionKey")]
        public string PositionKey { get; set; } = string.Empty;

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}