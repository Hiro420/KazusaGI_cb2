using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.Actions
{
    internal class SetGlobalPos : BaseAction
    {
        [JsonProperty] public readonly TargetType target;
        [JsonProperty] public readonly string key;
        [JsonProperty] public readonly BaseBornType born;
        [JsonProperty] public readonly bool setTarget;
    }
}
