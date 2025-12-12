using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public class AbilityModifier
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
    [JsonIgnore]
    public LocalIdGenerator? generator = null!;

    internal async Task Initialize(
        LocalIdGenerator idGenerator,
        IDictionary<uint, IInvocation> localIdToInvocationMap,
        IList<IInvocation> invokeSiteList)
    {
        generator = idGenerator;
        ushort configIndex = 0;
        // DO NOT CHANGE THE ORDER
        var tasks = new Task[]
            {
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAdded, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onRemoved, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onBeingHit, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAttackLanded, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onHittingOther, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onThinkInterval, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onKill, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onCrash, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAvatarIn, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAvatarOut, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onReconnect, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onChangeAuthority, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onVehicleIn, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onVehicleOut, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onZoneEnter, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onZoneExit, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onHeal, localIdToInvocationMap, invokeSiteList),
                InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onBeingHealed, localIdToInvocationMap, invokeSiteList),

            };
        await Task.WhenAll(tasks);

        if (modifierMixins == null) return;
        idGenerator.Type = ConfigAbilitySubContainerType.MODIFIER_MIXIN;
        ushort mixinIndex = 0;
        var tasks2 = new List<Task>();
        for (uint i = 0; i < modifierMixins.Length; i++)
        {
            idGenerator = new(ConfigAbilitySubContainerType.MODIFIER_MIXIN) { ConfigIndex = 0, MixinIndex = mixinIndex++ };
            tasks2.Add(modifierMixins[i].Initialize(idGenerator, localIdToInvocationMap, invokeSiteList));
        }
    }
    private async Task InitializeActionSubCategory(
        uint modifierIndex,
        ushort configIndex,
        BaseAction[]? actions,
        IDictionary<uint, IInvocation> localIdToInvocationMap,
        IList<IInvocation> invokeSiteList)
    {
        if (actions == null) return;
        await Task.Yield();
        LocalIdGenerator idGenerator = new(ConfigAbilitySubContainerType.MODIFIER_ACTION) { ConfigIndex = configIndex, ModifierIndex = modifierIndex };
        idGenerator.InitializeActionLocalIds(actions, localIdToInvocationMap, invokeSiteList);
    }
}
