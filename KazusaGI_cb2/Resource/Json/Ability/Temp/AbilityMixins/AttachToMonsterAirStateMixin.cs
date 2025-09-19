using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AttachToMonsterAirStateMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string modifierName;
        [JsonProperty] public readonly bool IsAirMove;
    }
}
