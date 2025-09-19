using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class ReplaceEventPatternMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string[] oldPatterns;
        [JsonProperty] public readonly string[] newPatterns;
    }
}
