using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class TriggerElementSupportMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly float duration;
        [JsonProperty] public readonly ElementType elementType;
    }
}
