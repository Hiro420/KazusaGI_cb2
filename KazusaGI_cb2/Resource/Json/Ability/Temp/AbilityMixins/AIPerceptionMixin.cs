using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins
{
    internal class AIPerceptionMixin : BaseAbilityMixin
    {
        [JsonProperty] public readonly int perceptionTemplateID;
        [JsonProperty] public readonly int[] featureTagIDs;
    }
}
