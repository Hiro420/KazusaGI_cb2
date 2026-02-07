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
	[JsonIgnore] public int fullNameHashCode { get; private set; }
	[JsonIgnore] public List<AbilitySpecialEntry> abilitySpecialVec { get; private set; } = new();

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
		abilitySpecialVec = new List<AbilitySpecialEntry>();

		fullNameHashCode = (abilityName + "::" + (overrideName ?? string.Empty)).GetHashCode();

		Dictionary<string, AbilityModifier>? modifiersMap = modifiers;
		if (defaultModifier != null)
		{
			var modifiersField = typeof(ConfigAbility).GetField(
				"modifiers",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			);

			if (modifiersMap == null)
			{
				modifiersMap = new Dictionary<string, AbilityModifier>();
				modifiersField?.SetValue(this, modifiersMap);
			}

			var modifierNameField = typeof(AbilityModifier).GetField(
				"modifierName",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			);
			modifierNameField?.SetValue(defaultModifier, DEFAULT_MODIFIER_NAME);
			modifiersMap.Add(DEFAULT_MODIFIER_NAME, defaultModifier);

			var applyModifier = new KazusaGI_cb2.Resource.Json.Ability.Temp.Actions.ApplyModifier();
			var applyModifierNameField = applyModifier.GetType().GetField(
				"modifierName",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			);
			applyModifierNameField?.SetValue(applyModifier, DEFAULT_MODIFIER_NAME);
			var applyTargetField = applyModifier.GetType().GetField(
				"target",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			);
			applyTargetField?.SetValue(applyModifier, "Self");

			var onAddedList = onAdded != null ? new List<BaseAction>(onAdded) : new List<BaseAction>();
			onAddedList.Insert(0, applyModifier);
			var onAddedField = typeof(ConfigAbility).GetField(
				"onAdded",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			);
			onAddedField?.SetValue(this, onAddedList.ToArray());
		}

		if (modifiersMap != null)
		{
			var modifierNameField = typeof(AbilityModifier).GetField(
				"modifierName",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
			);

			foreach (var kvp in modifiersMap)
			{
				if (kvp.Value == null)
					continue;
				modifierNameField?.SetValue(kvp.Value, kvp.Key);
			}
		}

		IterateAbilitySubActions(this, AddSubAction);
		IterateAbilitySubMixins(this, AddSubMixin);

		if (modifiersMap != null && modifiersMap.Count > 0)
		{
			var sortedModifierNames = modifiersMap.Keys.ToArray();
			Array.Sort(sortedModifierNames, StringComparer.Ordinal);
			modifierIDMap = new List<AbilityModifier>(sortedModifierNames.Length);

			for (int i = 0; i < sortedModifierNames.Length; i++)
			{
				if (!modifiersMap.TryGetValue(sortedModifierNames[i], out var modifier) || modifier == null)
				{
					modifierIDMap.Add(modifier);
					continue;
				}

				modifier.configLocalID = i;
				modifier.fullNameHashCode = (abilityName + "::" + (overrideName ?? string.Empty) + "::" + modifier.modifierName).GetHashCode();
				modifierIDMap.Add(modifier);

				IterateModifierSubActions(modifier, AddSubAction);
				IterateModifierSubMixins(modifier, AddSubMixin);
				ResolveModifierMPBehavior(modifier);
			}
		}

		if (abilitySpecials != null && abilitySpecials.Count > 0)
		{
			abilitySpecialVec = new List<AbilitySpecialEntry>(abilitySpecials.Count);
			var keys = abilitySpecials.Keys.ToArray();
			foreach (var key in keys)
			{
				var valueObj = abilitySpecials[key];
				if (valueObj is int intValue)
				{
					valueObj = (float)intValue;
					abilitySpecials[key] = valueObj;
				}
				else if (valueObj is long longValue)
				{
					valueObj = (float)longValue;
					abilitySpecials[key] = valueObj;
				}
				else if (valueObj is double doubleValue)
				{
					valueObj = (float)doubleValue;
					abilitySpecials[key] = valueObj;
				}
				else if (valueObj is decimal decimalValue)
				{
					valueObj = (float)decimalValue;
					abilitySpecials[key] = valueObj;
				}

				float value = 0f;
				if (valueObj is float floatValue)
					value = floatValue;
				else if (valueObj is string stringValue && float.TryParse(stringValue, out var parsedValue))
					value = parsedValue;

				abilitySpecialVec.Add(new AbilitySpecialEntry(key, KazusaGI_cb2.GameServer.Ability.Utils.AbilityHash(key), value));
			}
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

		invokeSites.Add(mixin);
		mixin.LocalID = invokeSites.Count - 1;
	}

	public readonly struct AbilitySpecialEntry
	{
		public AbilitySpecialEntry(string name, uint hash, float value)
		{
			Name = name;
			Hash = hash;
			Value = value;
		}

		public string Name { get; }
		public uint Hash { get; }
		public float Value { get; }
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
