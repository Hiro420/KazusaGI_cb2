using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class TriggerWitchTimeMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly TargetType ignoreTargetType;
        [JsonProperty] public readonly string weatherPattern;
    }
}
