using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AttackHittingSceneMixin : BaseAbilityMixin
{
    [JsonProperty] public bool? enableSceneHit;
    [JsonProperty] public string? hitPattern;
}