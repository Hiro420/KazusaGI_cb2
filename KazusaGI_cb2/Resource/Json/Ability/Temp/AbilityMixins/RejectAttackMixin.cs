using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class RejectAttackMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string attackTag;
    }
}
