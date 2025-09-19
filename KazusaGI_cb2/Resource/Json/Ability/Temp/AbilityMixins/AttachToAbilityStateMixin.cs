using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

public class AttachToAbilityStateMixin : BaseAbilityMixin
{
    [JsonProperty] public readonly AbilityState[] abilityStates;
    [JsonProperty] public readonly string modifierName;
}
