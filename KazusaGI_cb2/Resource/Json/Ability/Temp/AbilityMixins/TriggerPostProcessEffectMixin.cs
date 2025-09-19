using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class TriggerPostProcessEffectMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly string postEffectAssetName;
        [JsonProperty] public readonly float duration;
    }
}
