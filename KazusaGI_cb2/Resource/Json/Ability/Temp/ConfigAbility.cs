using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

/// <summary>
/// C# analogue of hk4e's ConfigAbilityImpl. This class is responsible for
/// building the sequential invoke-site list (invoke_site_vec) and the
/// modifier vector (modifier_vec) used by the runtime. The client-provided
/// LocalId is treated purely as an index into InvokeSiteList.
/// </summary>
public class ConfigAbility : BaseConfigAbility
{
    [JsonProperty] public readonly string abilityName;
    [JsonProperty] public readonly Dictionary<string, float>? abilitySpecials;
    [JsonProperty] public readonly BaseAbilityMixin[]? abilityMixins;
    [JsonProperty] public readonly Dictionary<string, AbilityModifier>? modifiers;
    [JsonProperty] public readonly BaseAction[]? onAdded;
    [JsonProperty] public readonly BaseAction[]? onRemoved;
    [JsonProperty] public readonly BaseAction[]? onAbilityStart;
    [JsonProperty] public readonly BaseAction[]? onKill;
    [JsonProperty] public readonly BaseAction[]? onFieldEnter;
    [JsonProperty] public readonly BaseAction[]? onFieldExit;
    [JsonProperty] public readonly BaseAction[]? onAttach;
    [JsonProperty] public readonly BaseAction[]? onDetach;
    [JsonProperty] public readonly BaseAction[]? onAvatarIn;
    [JsonProperty] public readonly BaseAction[]? onAvatarOut;
    [JsonProperty] public readonly bool isDynamicAbility; // if true, disable this ability by default. Enable via ConfigTalent AddAbility

    /// <summary>
    /// modifier_config_local_id -&gt; modifier config. Index (0..N-1) is the
    /// same ordering hk4e uses when building modifier_vec: modifiers are
    /// sorted lexicographically by name.
    /// </summary>
    [JsonIgnore] public SortedList<uint, AbilityModifier> ModifierList { get; private set; } = new();

    /// <summary>
    /// Sequential invoke-site list (invoke_site_vec). For a given ability
    /// instance, the client sends a LocalId which is an index into this list.
    /// </summary>
    [JsonIgnore] public List<IInvocation> InvokeSiteList { get; private set; } = new();

    internal Task Initialize()
    {
        // Rebuild invoke_site_vec and modifier_vec from scratch every time
        // to keep this method idempotent.
        InvokeSiteList.Clear();
        ModifierList.Clear();

        BuildAbilitySubActions();
        BuildAbilitySubMixins();
        BuildModifiers();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dumps the invoke_site_vec for debugging: shows index -&gt; invocation type.
    /// </summary>
    public void DebugAbility(Logger logger)
    {
        logger.LogInfo($"Ability '{abilityName}' invoke sites (count={InvokeSiteList.Count}):");
        for (int i = 0; i < InvokeSiteList.Count; i++)
        {
            var inv = InvokeSiteList[i];
            logger.LogInfo($"  [{i}] {inv.GetType().Name}");
        }

        if (ModifierList.Count > 0)
        {
            logger.LogInfo($"Ability '{abilityName}' modifiers (modifier_config_local_id -&gt; name):");
            foreach (var kv in ModifierList)
            {
                logger.LogInfo($"  [{kv.Key}] {kv.Value.modifierName}");
            }
        }
    }

    private void BuildAbilitySubActions()
    {
        // Mirror hk4e's iterateAbilitySubActions ordering.
        AddActionContainer(onAdded);
        AddActionContainer(onRemoved);
        AddActionContainer(onAbilityStart);
        AddActionContainer(onKill);
        AddActionContainer(onFieldEnter);
        AddActionContainer(onFieldExit);
        AddActionContainer(onAttach);
        AddActionContainer(onDetach);
        AddActionContainer(onAvatarIn);
        AddActionContainer(onAvatarOut);
    }

    private void BuildAbilitySubMixins()
    {
        if (abilityMixins == null || abilityMixins.Length == 0)
        {
            return;
        }

        foreach (var mixin in abilityMixins)
        {
            if (mixin != null)
            {
                InvokeSiteList.Add(mixin);
            }
        }
    }

    private void BuildModifiers()
    {
        if (modifiers == null || modifiers.Count == 0)
        {
            return;
        }

        // Build the sorted modifier name vector like hk4e's onLoaded:
        // take all keys, sort lexicographically, then map index -&gt; modifier.
        var names = modifiers.Keys
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        for (uint i = 0; i < names.Length; i++)
        {
            var name = names[i];
            if (!modifiers.TryGetValue(name, out var modifier) || modifier == null)
            {
                continue;
            }

            // Index i is the modifier_config_local_id.
            ModifierList.Add(i, modifier);

            AppendModifierInvokeSites(modifier);
        }
    }

    private void AddActionContainer(BaseAction[]? actions)
    {
        if (actions == null || actions.Length == 0)
        {
            return;
        }

        foreach (var action in actions)
        {
            if (action != null)
            {
                InvokeSiteList.Add(action);
            }
        }
    }

    private void AppendModifierInvokeSites(AbilityModifier modifier)
    {
        // Mirror hk4e's iterateModifierSubActions ordering.
        AddActionContainer(modifier.onAdded);
        AddActionContainer(modifier.onRemoved);
        AddActionContainer(modifier.onBeingHit);
        AddActionContainer(modifier.onAttackLanded);
        AddActionContainer(modifier.onHittingOther);
        AddActionContainer(modifier.onThinkInterval);
        AddActionContainer(modifier.onKill);
        AddActionContainer(modifier.onCrash);
        AddActionContainer(modifier.onAvatarIn);
        AddActionContainer(modifier.onAvatarOut);

        // Then mixins (iterateModifierSubMixins).
        if (modifier.modifierMixins == null || modifier.modifierMixins.Length == 0)
        {
            return;
        }

        foreach (var mixin in modifier.modifierMixins)
        {
            if (mixin != null)
            {
                InvokeSiteList.Add(mixin);
            }
        }
    }
}
