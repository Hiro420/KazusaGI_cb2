using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AvatarChangeSkillMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string priority;
        [JsonProperty] public readonly int jumpSkillID;
        [JsonProperty] public readonly int flySkillID;
    }
}
