using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class VelocityDetectMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly float minSpeed;
        [JsonProperty] public readonly float maxSpeed;
        [JsonProperty] public readonly BaseAction[] onNegedge;
    }
}
