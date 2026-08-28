using Newtonsoft.Json;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

/// <summary>
/// C# analogue of CB2 hk4e's server-side ConfigAbilityImpl.
/// Bake() mirrors ConfigAbilityImpl::onLoaded indexes consumed by AbilityComp.
/// </summary>
public class ConfigAbility : BaseConfigAbility
{
	[JsonProperty] public readonly string abilityName;
	[JsonProperty] public readonly string? useAbilityArgumentAsSpecialKey;
	[JsonProperty] public readonly string? setAbilityArgumentToOverrideMap;
	[JsonProperty] public readonly BaseAbilityMixin[]? abilityMixins;
	[JsonProperty] public Dictionary<string, AbilityScalarValue>? abilitySpecials { get; private set; }
	[JsonProperty] public Dictionary<string, AbilityModifier>? modifiers { get; private set; }
	[JsonProperty] public readonly AbilityModifier? defaultModifier;
	[JsonProperty] public BaseAction[]? onAdded { get; private set; }
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
	[JsonIgnore] public List<AbilitySpecialEntry> abilitySpecialVec { get; private set; } = new();


	/// <summary>
	/// Compatibility entry point for existing resource initialization callers.
	/// </summary>
	internal Task Initialize()
	{
		Bake();
		return Task.CompletedTask;
	}

	/// <summary>
	/// Deterministic bake of the transient config indexes consumed by hk4e-like
	/// runtime ability dispatch. Re-running Bake() is intentionally idempotent.
	/// </summary>
	public void Bake()
	{
		invokeSites = new List<IInvocation>();
		modifierIDMap = new List<AbilityModifier>();
		abilitySpecialVec = new List<AbilitySpecialEntry>();

		// hk4e ConfigAbilityImpl::onLoaded keeps defaultModifier separate.
		// Only entries in the named modifiers map participate in modifier_vec/local IDs.
		Dictionary<string, AbilityModifier>? modifiersMap = modifiers;

		if (modifiersMap != null)
		{
			foreach ((string name, AbilityModifier modifier) in modifiersMap)
			{
				if (modifier != null)
					modifier.modifierName = name;
			}
		}

		IterateAbilitySubActions(this, AddSubAction);
		IterateAbilitySubMixins(this, AddSubMixin);

		if (modifiersMap != null && modifiersMap.Count > 0)
		{
			string[] sortedModifierNames = modifiersMap.Keys.OrderBy(x => x, StringComparer.Ordinal).ToArray();
			modifierIDMap = new List<AbilityModifier>(sortedModifierNames.Length);

			for (int i = 0; i < sortedModifierNames.Length; i++)
			{
				AbilityModifier modifier = modifiersMap[sortedModifierNames[i]]
					?? throw new InvalidDataException($"Ability '{abilityName}' contains null modifier '{sortedModifierNames[i]}'.");

				modifier.configLocalID = i;
				modifierIDMap.Add(modifier);

				IterateModifierSubActions(modifier, AddSubAction);
				IterateModifierSubMixins(modifier, AddSubMixin);
				ResolveModifierMPBehavior(modifier);
			}
		}

		if (abilitySpecials != null && abilitySpecials.Count > 0)
		{
			// ConfigAbilityImpl::onLoaded bakes ability_special_vec as
			// tuple<string, int32 hash, float>. The source DynamicArgument map is
			// kept lossless, but this fast vector accepts only float exactly as hk4e.
			foreach ((string key, AbilityScalarValue value) in abilitySpecials)
			{
				if (value.Kind != AbilityScalarValueKind.Float)
					throw new InvalidDataException(
						$"Ability '{abilityName}' special '{key}' is {value.Kind}; hk4e ConfigAbilityImpl::onLoaded requires float.");

				uint hash = KazusaGI_cb2.GameServer.Ability.Utils.AbilityHash(key);
				abilitySpecialVec.Add(new AbilitySpecialEntry(key, hash, value.FloatValue));
			}
		}
	}

	// Kept for callers created before the loader rewrite.
	internal void OnBakeLoaded() => Bake();

	/// <summary>
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::_IterateSubActions()
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
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::IterateAbilitySubActions()
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
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::IterateAbilitySubMixins()
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
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::IterateModifierSubActions()
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
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::IterateModifierSubMixins()
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
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::ResolveModifierMPBehavior()
	/// Determines multiplayer behavior for modifiers.
	/// </summary>
	private void ResolveModifierMPBehavior(AbilityModifier modifier)
	{
		// CB2 hk4e ConfigAbilityImpl::resolveModifierMpBehavior is intentionally empty.
	}

	/// <summary>
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::AddSubAction()
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
	/// CB2 hk4e server equivalent of ConfigAbilityImpl::AddSubMixin()
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
