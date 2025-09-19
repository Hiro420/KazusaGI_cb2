using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

public class DoActionByElementReactionMixin : BaseAbilityMixin
{
    [JsonProperty] public readonly EntityType[] entityTypes;
    [JsonProperty] public readonly ReactionType[] reactionTypes;
    [JsonProperty] public readonly BaseAction[]? actions;
}
