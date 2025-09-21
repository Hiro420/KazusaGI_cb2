using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

internal class AnimatorRotationCompensateMixin : BaseAbilityMixin
{
    [JsonProperty] public float? rotationSpeed;
    [JsonProperty] public bool? useLocalRotation;
}