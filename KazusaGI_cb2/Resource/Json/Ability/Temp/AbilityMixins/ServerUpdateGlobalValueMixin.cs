using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ServerUpdateGlobalValueMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string key;
    }
}
