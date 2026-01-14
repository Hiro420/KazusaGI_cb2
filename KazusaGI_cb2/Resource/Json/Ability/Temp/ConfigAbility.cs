using System.Reflection;
using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

/// <summary>
/// C# analogue of hk4e's ConfigAbilityImpl (MoleMole.Config.ConfigAbility).
/// Exact replication of the game client's OnBakeLoaded initialization.
/// </summary>
public class ConfigAbility : BaseConfigAbility
{
	[JsonProperty] public readonly string abilityName;
	[JsonProperty] public readonly string? useAbilityArgumentAsSpecialKey;
	[JsonProperty] public readonly string? setAbilityArgumentToOverrideMap;
	[JsonProperty] public readonly BaseAbilityMixin[]? abilityMixins;
	[JsonProperty] public readonly Dictionary<string, object>? abilitySpecials;
	[JsonProperty] public readonly Dictionary<string, AbilityModifier>? modifiers;
	[JsonProperty] public readonly AbilityModifier? defaultModifier;
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
	[JsonProperty] public readonly Dictionary<string, object>? abilityDefinedProperties;
	[JsonProperty] public readonly bool isDynamicAbility;

	// Runtime state (NonSerialized equivalent)
	[JsonIgnore] public List<IInvocation> invokeSites { get; private set; } = new();
	[JsonIgnore] public List<AbilityModifier> modifierIDMap { get; private set; } = new();
	[JsonIgnore] public string? overrideName { get; set; }

	private const string DEFAULT_MODIFIER_NAME = "__DEFAULT_MODIFIER";

	/// <summary>
	/// Public initialization method for compatibility.
	/// Calls OnBakeLoaded() to perform the exact game client initialization.
	/// </summary>
	internal Task Initialize()
	{
		OnBakeLoaded();
		return Task.CompletedTask;
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::OnBakeLoaded()
	/// Initializes invokeSites and modifierIDMap exactly as the game client does.
	/// </summary>
	internal void OnBakeLoaded()
	{
		invokeSites = new List<IInvocation>();
		modifierIDMap = new List<AbilityModifier>();

		// 1. Process abilitySpecials (lines 35-66 in pseudocode)
		// The game does hash processing here but we'll keep strings for now

		// 2. Add defaultModifier to modifiers dictionary if it exists
		if (defaultModifier != null)
		{
			var modDict = modifiers != null 
				? new Dictionary<string, AbilityModifier>(modifiers) 
				: new Dictionary<string, AbilityModifier>();
			
			modDict[DEFAULT_MODIFIER_NAME] = defaultModifier;
			
			// Replace the readonly field via reflection (hack but necessary for exact match)
			var field = typeof(ConfigAbility).GetField("modifiers", 
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			field?.SetValue(this, modDict);
		}

		// 3. Call ResolveModifierMPBehavior on each modifier
		if (modifiers != null)
		{
			foreach (var kvp in modifiers)
			{
				ResolveModifierMPBehavior(kvp.Value);
			}
		}

		// 4. Build sorted list of modifier names (lexicographical order)
		string[]? sortedModifierNames = null;
		if (modifiers != null && modifiers.Count > 0)
		{
			sortedModifierNames = modifiers.Keys.ToArray();
			Array.Sort(sortedModifierNames, StringComparer.Ordinal);

			// Build modifierIDMap in sorted order
			foreach (var name in sortedModifierNames)
			{
				if (modifiers.TryGetValue(name, out var modifier))
				{
					modifierIDMap.Add(modifier);
				}
			}
		}

		// 5. Iterate ability sub-actions (adds to invokeSites)
		// Note: This happens BEFORE modifier iteration in the game
		IterateAbilitySubActions(this, AddSubAction);

		// 6. Iterate ability sub-mixins (adds to invokeSites)
		// Note: This also happens BEFORE modifier iteration
		IterateAbilitySubMixins(this, AddSubMixin);

		// 7. Iterate modifier sub-actions and sub-mixins
		// The game iterates these AFTER ability actions/mixins
		if (sortedModifierNames != null && modifiers != null)
		{
			int debugCount = 0;
			foreach (var name in sortedModifierNames)
			{
				if (!modifiers.TryGetValue(name, out var modifier))
					continue;

				int beforeCount = invokeSites.Count;
				IterateModifierSubActions(modifier, AddSubAction);
				IterateModifierSubMixins(modifier, AddSubMixin);
				int afterCount = invokeSites.Count;
				
				if (afterCount > beforeCount)
				{
					debugCount++;
				}
			}
			
			//if (debugCount > 0 && invokeSites.Count > 0)
			//{
			//	Console.WriteLine($"[DEBUG] {abilityName}: Added {invokeSites.Count} invoke sites from {debugCount} modifiers");
			//}
		}
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::_IterateSubActions()
	/// Recursively iterates actions, calls callback, then processes sub-actions.
	/// </summary>
	private void _IterateSubActions(BaseAction[]? actions, Action<BaseAction> callback)
	{
		if (actions == null || actions.Length == 0)
			return;

		var subActionsList = new List<BaseAction[]>();

		foreach (var action in actions)
		{
			if (action == null)
				continue;

			// Invoke callback first
			callback(action);

			// Clear and get sub-actions
			subActionsList.Clear();
			action.GetSubActions(subActionsList);

			// Recursively iterate sub-actions
			foreach (var subArray in subActionsList)
			{
				_IterateSubActions(subArray, callback);
			}
		}
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::IterateAbilitySubActions()
	/// Iterates all action arrays in the ability.
	/// </summary>
	private void IterateAbilitySubActions(ConfigAbility ability, Action<BaseAction> callback)
	{
		_IterateSubActions(ability.onAdded, callback);
		_IterateSubActions(ability.onRemoved, callback);
		_IterateSubActions(ability.onAbilityStart, callback);
		_IterateSubActions(ability.onKill, callback);
		_IterateSubActions(ability.onFieldEnter, callback);
		_IterateSubActions(ability.onFieldExit, callback);
		_IterateSubActions(ability.onAttach, callback);
		_IterateSubActions(ability.onDetach, callback);
		_IterateSubActions(ability.onAvatarIn, callback);
		_IterateSubActions(ability.onAvatarOut, callback);
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::IterateAbilitySubMixins()
	/// Iterates ability mixins and their sub-actions.
	/// </summary>
	private void IterateAbilitySubMixins(ConfigAbility ability, Action<BaseAbilityMixin> callback)
	{
		if (ability.abilityMixins == null || ability.abilityMixins.Length == 0)
			return;

		var subActionsList = new List<BaseAction[]>();

		foreach (var mixin in ability.abilityMixins)
		{
			if (mixin == null)
				continue;

			// Invoke callback for the mixin
			callback(mixin);

			// Get sub-actions from mixin and iterate them
			subActionsList.Clear();
			mixin.GetSubActions(subActionsList);

			foreach (var subArray in subActionsList)
			{
				_IterateSubActions(subArray, AddSubAction);
			}
		}
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::IterateModifierSubActions()
	/// Iterates all action arrays in a modifier.
	/// </summary>
	private void IterateModifierSubActions(AbilityModifier modifier, Action<BaseAction> callback)
	{
		if (modifier == null)
			return;
			
		_IterateSubActions(modifier.onAdded, callback);
		_IterateSubActions(modifier.onRemoved, callback);
		_IterateSubActions(modifier.onBeingHit, callback);
		_IterateSubActions(modifier.onAttackLanded, callback);
		_IterateSubActions(modifier.onHittingOther, callback);
		_IterateSubActions(modifier.onThinkInterval, callback);
		_IterateSubActions(modifier.onKill, callback);
		_IterateSubActions(modifier.onCrash, callback);
		_IterateSubActions(modifier.onAvatarIn, callback);
		_IterateSubActions(modifier.onAvatarOut, callback);
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::IterateModifierSubMixins()
	/// Iterates modifier mixins and their sub-actions.
	/// </summary>
	private void IterateModifierSubMixins(AbilityModifier modifier, Action<BaseAbilityMixin> callback)
	{
		if (modifier.modifierMixins == null || modifier.modifierMixins.Length == 0)
			return;

		var subActionsList = new List<BaseAction[]>();

		foreach (var mixin in modifier.modifierMixins)
		{
			if (mixin == null)
				continue;

			// Invoke callback for the mixin
			callback(mixin);

			// Get sub-actions from mixin and iterate them
			subActionsList.Clear();
			mixin.GetSubActions(subActionsList);

			foreach (var subArray in subActionsList)
			{
				_IterateSubActions(subArray, AddSubAction);
			}
		}
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::ResolveModifierMPBehavior()
	/// Determines multiplayer behavior for modifiers.
	/// </summary>
	private void ResolveModifierMPBehavior(AbilityModifier modifier)
	{
		if (modifier == null)
			return;

		bool canBeMPEligible = true;

		// Check conditions that disqualify MP eligibility
		if (modifier.state != null)
			canBeMPEligible = false;
		
		if (modifier.properties != null && modifier.properties.Count > 0)
			canBeMPEligible = false;
		
		if (modifier.elementType != null)
			canBeMPEligible = false;
		
		if (modifier.isUnique == true)
			canBeMPEligible = false;

		if (!canBeMPEligible)
		{
			// modifier.mpBehavior remains 0 (default)
			return;
		}

		// Check actions and mixins to see if they disqualify MP
		bool hasDisqualifyingContent = false;

		IterateModifierSubActions(modifier, action =>
		{
			if (action.CheckActionNeedServer())
				hasDisqualifyingContent = true;
		});

		IterateModifierSubMixins(modifier, mixin =>
		{
			if (mixin.NeedServer())
				hasDisqualifyingContent = true;
		});

		if (!hasDisqualifyingContent)
		{
			// Set mpBehavior to 1 (client-only)
			// We need to modify the readonly field
			var field = typeof(AbilityModifier).GetField("mpBehavior",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
			field?.SetValue(modifier, 1);
		}
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::AddSubAction()
	/// Adds action to invokeSites and assigns localID.
	/// </summary>
	private void AddSubAction(BaseAction action)
	{
		if (action == null)
			return;

		// Skip if already added (same instance referenced multiple times)
		if (action.LocalID != -1)
		{
			return;
		}

		invokeSites.Add(action);
		action.LocalID = invokeSites.Count - 1;
	}

	/// <summary>
	/// EXACT replication of MoleMole.Config.ConfigAbility::AddSubMixin()
	/// Adds mixin to invokeSites and assigns localID.
	/// </summary>
	private void AddSubMixin(BaseAbilityMixin mixin)
	{
		if (mixin == null)
			return;

		// Skip if already added (same instance referenced multiple times)
		if (mixin.LocalID != -1)
		{
			return;
		}

		invokeSites.Add(mixin);
		mixin.LocalID = invokeSites.Count - 1;
	}

	/// <summary>
	/// Debug helper to dump invoke sites and modifiers
	/// </summary>
	public void DebugAbility(Logger logger)
	{
		logger.LogInfo($"Ability '{abilityName}' invoke sites (count={invokeSites.Count}):");
		for (int i = 0; i < invokeSites.Count; i++)
		{
			var inv = invokeSites[i];
			logger.LogInfo($"  [{i}] {inv.GetType().Name}");
		}

		if (modifierIDMap.Count > 0)
		{
			logger.LogInfo($"Ability '{abilityName}' modifiers (count={modifierIDMap.Count}):");
			for (int i = 0; i < modifierIDMap.Count; i++)
			{
				logger.LogInfo($"  [{i}] {modifierIDMap[i].modifierName}");
			}
		}
	}
}
