using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class ServerLuaCall : BaseAction
    {
        [JsonProperty] public readonly LuaCallType luaCallType;
        [JsonProperty] public readonly string funcName;
        [JsonProperty] public readonly float param1;
        [JsonProperty] public readonly float param2;
        [JsonProperty] public readonly float param3;
    }
}
