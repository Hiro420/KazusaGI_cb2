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

        // DO NOT CHANGE THE ORDER. This mirrors
        // ConfigAbilityImpl::iterateModifierSubActions in hk4e.
        ushort configIndex = 0;

        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAdded, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onRemoved, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onBeingHit, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAttackLanded, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onHittingOther, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onThinkInterval, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onKill, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onCrash, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAvatarIn, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onAvatarOut, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onReconnect, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onChangeAuthority, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onVehicleIn, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onVehicleOut, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onZoneEnter, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onZoneExit, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onHeal, localIdToInvocationMap, invokeSiteList);
        await InitializeActionSubCategory(idGenerator.ModifierIndex, configIndex++, onBeingHealed, localIdToInvocationMap, invokeSiteList);

        // Then process modifier mixins in the same
        // order as ConfigAbilityImpl::iterateModifierSubMixins.
        if (modifierMixins == null)
        {
            return;
        }

        ushort mixinIndex = 0;
        for (uint i = 0; i < modifierMixins.Length; i++)
        {
            var mixinGenerator = new LocalIdGenerator(ConfigAbilitySubContainerType.MODIFIER_MIXIN)
            {
                ConfigIndex = 0,
                MixinIndex = mixinIndex++,
                ModifierIndex = idGenerator.ModifierIndex
            };

            await modifierMixins[i].Initialize(mixinGenerator, localIdToInvocationMap, invokeSiteList);
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
