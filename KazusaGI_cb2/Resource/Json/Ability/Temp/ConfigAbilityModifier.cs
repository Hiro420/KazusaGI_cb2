using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public class ConfigAbilityModifier
{
    [JsonProperty] public readonly StackingType stacking;
    [JsonProperty] public readonly string modifierName;
    [JsonProperty] public readonly bool? isUnique;
    [JsonProperty] public readonly object duration;
    [JsonProperty] public readonly Dictionary<PropertyModifierType, object> properties;
    [JsonProperty] public readonly bool? isLimitedProperties;
    [JsonProperty] public readonly string elementDurability;
    [JsonProperty] public readonly object thinkInterval;
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
    [JsonProperty] public readonly BaseAction[]? onReconnect;
    [JsonProperty] public readonly BaseAction[]? onChangeAuthority;
    [JsonProperty] public readonly BaseAction[]? onVehicleIn;
    [JsonProperty] public readonly BaseAction[]? onVehicleOut;
    [JsonProperty] public readonly BaseAction[]? onZoneEnter;
    [JsonProperty] public readonly BaseAction[]? onZoneExit;
    [JsonProperty] public readonly BaseAction[]? onHeal;
    [JsonProperty] public readonly BaseAction[]? onBeingHealed;

    public enum State
    {
        None,
        Invincible,
        Limbo
    }

    [JsonProperty] public readonly State state;
}