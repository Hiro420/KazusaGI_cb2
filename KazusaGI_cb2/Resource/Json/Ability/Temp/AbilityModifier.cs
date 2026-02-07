using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

/// <summary>
/// C# analogue of hk4e's ConfigAbilityModifier.
/// EXACT match to game client structure.
/// </summary>
public class AbilityModifier
{
    [JsonProperty] public readonly StackingType stacking;
    [JsonProperty] public readonly string modifierName;
    [JsonProperty] public readonly bool? isUnique;
    [JsonProperty] public readonly object? duration;
    [JsonProperty] public readonly Dictionary<PropertyModifierType, object>? properties;
    [JsonProperty] public readonly bool? isLimitedProperties;
    [JsonProperty] public readonly string? elementDurability;
    [JsonProperty] public readonly object? thinkInterval;
    [JsonProperty] public readonly BaseAbilityMixin[]? modifierMixins;
    [JsonProperty] public readonly BaseAction[]? onAdded;
    [JsonProperty] public readonly BaseAction[]? onRemoved;
    [JsonProperty] public readonly BaseAction[]? onBeingHit;
    [JsonProperty] public readonly BaseAction[]? onAttackLanded;
    [JsonProperty] public readonly BaseAction[]? onHittingOther;
    [JsonProperty] public readonly BaseAction[]? onThinkInterval;
    [JsonProperty] public readonly BaseAction[]? onKill;
    [JsonProperty] public readonly BaseAction[]? onCrash;
    [JsonProperty] public readonly BaseAction[]? onAvatarIn;
    [JsonProperty] public readonly BaseAction[]? onAvatarOut;

    // Additional fields from game client
    [JsonProperty] public readonly object? state;
    [JsonProperty] public readonly object? elementType;
    
    // Runtime field - mpBehavior (0 = default, 1 = client-only)
    [JsonIgnore] public int mpBehavior { get; internal set; } = 0;
    [JsonIgnore] public int configLocalID { get; internal set; } = -1;
    [JsonIgnore] public int fullNameHashCode { get; internal set; }
}
