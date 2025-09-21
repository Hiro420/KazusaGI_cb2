using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes
{
    public class ConfigBornByActionPoint : BaseBornType
    {
        [JsonProperty("actionPointID")]
        public string ActionPointID { get; set; } = string.Empty;

        [JsonProperty("useActionPointRotation")]
        public bool UseActionPointRotation { get; set; }

        [JsonProperty("offset")]
        public object Offset { get; set; } = new object();
    }
}