using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    public class CallLuaTask : BaseAction
    {
        [JsonProperty("luaTask")]
        public string LuaTask { get; set; } = string.Empty;

        [JsonProperty("parameters")]
        public object Parameters { get; set; } = new object();

        [JsonProperty("target")]
        public object Target { get; set; } = new object();
    }
}