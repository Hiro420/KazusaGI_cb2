using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class CurLocalAvatarMixinV2 : BaseAbilityMixin
    {
        [JsonProperty] public readonly string modifierName;
    }
}
