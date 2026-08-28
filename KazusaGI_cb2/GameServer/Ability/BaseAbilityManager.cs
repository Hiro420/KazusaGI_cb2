using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;
using ProtoBuf;

namespace KazusaGI_cb2.GameServer.Systems.Ability;

public abstract class BaseAbilityManager
{
	protected static Logger logger = new("AbilityManager");
	protected readonly Entity Owner;

	// hk4e AbilityComp::applied_ability_map_: authoritative runtime ability state.
	// InstancedAbilityHashMap remains only as a compatibility view for older callers
	// while the rest of the ability subsystem is migrated in later checkpoints.
	protected readonly SortedDictionary<uint, ActorAbility> AppliedAbilityMap = new();
	protected readonly HashSet<uint> AbilityNameHashSet = new();
	protected readonly Dictionary<uint, uint> InstancedAbilityHashMap = new();
	public abstract SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } // <abilityNameHash, configAbility>
	public readonly Dictionary<uint, Dictionary<uint, float>> AbilitySpecialOverrideMap = new(); // <abilityNameHash, <abilitySpecialNameHash, value>>
	public abstract Dictionary<string, Dictionary<string, float>?>? AbilitySpecials { get; }// <abilityName, <abilitySpecial, value>>
	public abstract HashSet<string> ActiveDynamicAbilities { get; }
	public abstract Dictionary<string, HashSet<string>> UnlockedTalentParams { get; }
	protected Dictionary<uint, string> AbilitySpecialHashMap = new(); // <hash, abilitySpecialName>

	protected Dictionary<uint, float> GlobalValueHashMap = new(); // <hash, value> TODO map the hashes to variable names
	protected Dictionary<int, ActiveModifierInfo> ActiveModifiers = new(); // <modifierLocalId, modifierInfo>

	// hk4e AbilityComp::applied_modifier_vec_: modifier id N lives at slot N-1.
	// Null slots are preserved after removal and are part of the runtime state.
	protected readonly List<ActorModifier?> AppliedModifierVec = new();
	public bool _isInitialized { get; private set; } = false;

	protected BaseAbilityManager(Entity owner)
	{
		Owner = owner;
	}

	public virtual void Initialize()
	{
		// Process abilitySpecials and build hash maps
		if (AbilitySpecials != null)
		{
			foreach (var ability in AbilitySpecials)
			{
				uint ablHash = GameServer.Ability.Utils.AbilityHash(ability.Key);
				AbilitySpecialOverrideMap[ablHash] = new();
				if (ability.Value != null)
				{
					foreach (var special in ability.Value)
					{
						AbilitySpecialOverrideMap[ablHash][GameServer.Ability.Utils.AbilityHash(special.Key)] = special.Value;
						AbilitySpecialHashMap[GameServer.Ability.Utils.AbilityHash(special.Key)] = special.Key;
					}
				}
			}
		}

		// Preserve the exact ordering this server used before this checkpoint while
		// making the ids authoritative runtime state. Monster/static discovery order
		// itself is migrated to hk4e in the next checkpoint.
		if (AppliedAbilityMap.Count == 0 &&
			this is not AvatarAbilityManager &&
			this is not TeamAbilityManager)
		{
			foreach (ConfigAbility configAbility in ConfigAbilityHashMap.Values.ToArray())
			{
				if (configAbility != null)
					AttachActorAbility(configAbility);
			}
		}

		// ConfigAbility.Initialize() already calls OnBakeLoaded() during resource loading
		// Just log the ability info here for debugging
		foreach (var kvp in ConfigAbilityHashMap)
		{
			var configAbility = kvp.Value;
			if (configAbility == null)
				continue;

			//logger.LogSuccess($"Using ConfigAbility '{configAbility.abilityName}' - {configAbility.invokeSites.Count} invoke sites, {configAbility.modifierIDMap.Count} modifiers");
		}
		_isInitialized = true;

	}

	public virtual AbilitySyncStateInfo BuildAbilitySyncStateInfo()
	{
		var syncInfo = new Protocol.AbilitySyncStateInfo
		{
			IsInited = true
		};

		// hk4e AbilityComp::toClient serializes applied_ability_map_. IDs are
		// allocated when ActorAbility is attached and are never fabricated here.
		if (this is not AvatarAbilityManager && this is not TeamAbilityManager)
		{
			foreach (ActorAbility actorAbility in AppliedAbilityMap.Values)
				syncInfo.AppliedAbilities.Add(actorAbility.ToProtocol());
		}

		// Compatibility path retained until the dynamic-value runtime checkpoint.
		foreach (var kvp in AbilitySpecialOverrideMap)
		{
			foreach (var specialKvp in kvp.Value)
			{
				syncInfo.DynamicValueMaps.Add(new Protocol.AbilityScalarValueEntry
				{
					Key = new Protocol.AbilityString { Hash = specialKvp.Key },
					ValueType = AbilityScalarType.AbilityScalarTypeFloat,
					FloatValue = specialKvp.Value
				});
			}
		}

		foreach (var kvp in GlobalValueHashMap)
		{
			syncInfo.DynamicValueMaps.Add(new Protocol.AbilityScalarValueEntry
			{
				Key = new Protocol.AbilityString { Hash = kvp.Key },
				ValueType = AbilityScalarType.AbilityScalarTypeFloat,
				FloatValue = kvp.Value
			});
		}

		AppendAppliedModifiers(syncInfo);

		return syncInfo;
	}

	public virtual async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		MemoryStream data = new MemoryStream(invoke.AbilityData);

		if (invoke.Head.LocalId != 255) // INVOCATION_META_LOCALID = 255
		{

			logger.LogInfo($"Server-sided ability invoke: LocalId={invoke.Head.LocalId}, " +
				$"ArgumentType={invoke.ArgumentType}, EntityId={invoke.EntityId}, TargetId={invoke.Head.TargetId}");

			// Mirror hk4e's serverCommonInvokeHandler/commonInvokeEntryDispatch:
			// resolve (ability, modifier) first, then treat LocalId as a pure
			// index into the ability's invokeSites list.
			if (!TryResolveAbilityForInvoke(invoke, out var ability, out var modifierController))
			{
				return;
			}

			int localId = invoke.Head.LocalId;
			if (localId < 0 || localId >= ability.invokeSites.Count)
			{
				logger.LogWarning($"Invalid invoke-site LocalId={localId} for ability {ability.abilityName} " +
					$"(instancedAbilityId={invoke.Head.InstancedAbilityId}, invokeSiteCount={ability.invokeSites.Count})");
				ability.DebugAbility(logger);
				return;
			}

			var invocation = ability.invokeSites[localId];

			logger.LogSuccess($"Invoking ability: {ability.abilityName}, LocalId: {localId} | {invocation.GetType().Name}");
			Entity? entity2invoke = null;
			EntityManager entityManager = Owner.session.player.Scene.EntityManager;

			if (invoke.EntityId != 0)
				entityManager.TryGet(invoke.EntityId, out entity2invoke);
			if (entity2invoke == null)
				entity2invoke = Owner;

			await invocation.Invoke(invoke, ability.abilityName, entity2invoke, null);

			return;
		}

		//TODO add all cases
		switch (invoke.ArgumentType)
		{
			case AbilityInvokeArgument.AbilityNone:
				// hk4e treats this as a meta marker with no
				// additional server-side behavior; ignore.
				break;
			case AbilityInvokeArgument.AbilityMetaModifierChange:
				AbilityMetaModifierChange info = Serializer.Deserialize<AbilityMetaModifierChange>(data);
				ProcessModifierAction(invoke, info);
				break;
			case AbilityInvokeArgument.AbilityMetaSpecialFloatArgument:
				AbilityMetaSpecialFloatArgument info2 = Serializer.Deserialize<AbilityMetaSpecialFloatArgument>(data);
				break;
			case AbilityInvokeArgument.AbilityMetaOverrideParam:
				AbilityScalarValueEntry asEntri = Serializer.Deserialize<AbilityScalarValueEntry>(data);
				if (!TryResolveAbilityForInvoke(invoke, out var overrideAbility, out _))
				{
					logger.LogWarning($"AbilityMetaOverrideParam: failed to resolve ability for instancedAbilityId {invoke.Head.InstancedAbilityId}");
					break;
				}
				uint overrideAbilityHash = GameServer.Ability.Utils.AbilityHash(overrideAbility.abilityName);
				if (!AbilitySpecialOverrideMap.TryGetValue(overrideAbilityHash, out var specialsMap))
				{
					specialsMap = new Dictionary<uint, float>();
					AbilitySpecialOverrideMap[overrideAbilityHash] = specialsMap;
				}
				specialsMap[asEntri.Key.Hash] = asEntri.FloatValue;
				break;
			case AbilityInvokeArgument.AbilityMetaReinitOverridemap:
				AbilityMetaReInitOverrideMap info3 = Serializer.Deserialize<AbilityMetaReInitOverrideMap>(data);
				if (!TryResolveAbilityForInvoke(invoke, out var reinitAbility, out _))
				{
					logger.LogWarning($"AbilityMetaReinitOverridemap: failed to resolve ability for instancedAbilityId {invoke.Head.InstancedAbilityId}");
					break;
				}
				uint reinitAbilityHash = GameServer.Ability.Utils.AbilityHash(reinitAbility.abilityName);
				ReInitOverrideMap(reinitAbilityHash, info3 as AbilityMetaReInitOverrideMap);
				break;
			case AbilityInvokeArgument.AbilityMetaGlobalFloatValue:
				AbilityScalarValueEntry asEntry = Serializer.Deserialize<AbilityScalarValueEntry>(data);
				GlobalValueHashMap[asEntry.Key.Hash] = asEntry.FloatValue;
				break;
			case AbilityInvokeArgument.AbilityMetaSetKilledSetate:
				AbilityMetaSetKilledState abilityMetaSetKilledState = Serializer.Deserialize<AbilityMetaSetKilledState>(data);
				if (abilityMetaSetKilledState.Killed)
				{
					Owner.ForceKill();
				}
				break;
			case AbilityInvokeArgument.AbilityMetaAddOrGetAbilityAndTrigger:
				// In hk4e this can either fetch an existing applied ability
				// or create a new one, then trigger it. For caching purposes
				// we only need to bind the instancedAbilityId from the head
				// to the ability name/override coming in this payload.
				AbilityMetaAddOrGetAbilityAndTrigger info4 = Serializer.Deserialize<AbilityMetaAddOrGetAbilityAndTrigger>(data);
				if (info4 != null)
				{
					var applied = new AbilityAppliedAbility
					{
						AbilityName = info4.AbilityName ?? new AbilityString(),
						AbilityOverride = info4.AbilityOverride ?? new AbilityString(),
						InstancedAbilityId = invoke.Head.InstancedAbilityId
					};

					AddAbility(applied);
				}
				break;
			case AbilityInvokeArgument.AbilityMetaAddNewAbility:
				AbilityMetaAddAbility info5 = Serializer.Deserialize<AbilityMetaAddAbility>(data);
				AddAbility(info5.Ability);
				break;
			case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
				AbilityMetaModifierDurabilityChange info6 = Serializer.Deserialize<AbilityMetaModifierDurabilityChange>(data);
				break;
			case AbilityInvokeArgument.AbilityMetaSetModifierApplyEntity:
				AbilityMetaSetModifierApplyEntityId setApplyInfo = Serializer.Deserialize<AbilityMetaSetModifierApplyEntityId>(data);
				HandleSetModifierApplyEntity(invoke, setApplyInfo);
				break;
			case AbilityInvokeArgument.AbilityActionTriggerAbility:
				AbilityActionTriggerAbility info7 = Serializer.Deserialize<AbilityActionTriggerAbility>(data);
				break;
			case AbilityInvokeArgument.AbilityActionGenerateElemBall:
				AbilityActionGenerateElemBall info8 = Serializer.Deserialize<AbilityActionGenerateElemBall>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinWindZone:
				AbilityMixinWindZone info9 = Serializer.Deserialize<AbilityMixinWindZone>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinCostStamina:
				AbilityMixinCostStamina info10 = Serializer.Deserialize<AbilityMixinCostStamina>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinGlobalShield:
				AbilityMixinGlobalShield info11 = Serializer.Deserialize<AbilityMixinGlobalShield>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinWindSeedSpawner:
				AbilityMixinWindSeedSpawner info12 = Serializer.Deserialize<AbilityMixinWindSeedSpawner>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinShieldBar:
				await HandleMixinInvokeAsync<ShieldBarMixin>(invoke);
				break;
			default:
				logger.LogWarning($"Unhandled AbilityInvokeArgument: {invoke.ArgumentType}");
				break;
		}
	}

	protected virtual async Task HandleMixinInvokeAsync<TMixin>(AbilityInvokeEntry invoke)
		where TMixin : BaseAbilityMixin
	{
		if (!TryResolveAbilityForInvoke(invoke, out ConfigAbility ability, out ActorModifier? _))
		{
			logger.LogWarning($"HandleMixinInvokeAsync<{typeof(TMixin).Name}>: failed to resolve ability for instancedAbilityId {invoke.Head.InstancedAbilityId}");
			return;
		}

		BaseAbilityMixin? mixin = null;
		if (ability.abilityMixins != null)
		{
			foreach (var m in ability.abilityMixins)
			{
				if (m is TMixin)
				{
					mixin = m;
					break;
				}
			}
		}

		if (mixin == null)
		{
			logger.LogWarning($"HandleMixinInvokeAsync<{typeof(TMixin).Name}>: no mixin instance found on ability {ability.abilityName}");
			return;
		}

		var handler = AbilityMixinHandlerRegistry.GetHandlerForMixin(mixin);
		if (handler == null)
		{
			logger.LogWarning($"HandleMixinInvokeAsync<{typeof(TMixin).Name}>: no handler registered");
			return;
		}

		try
		{
			bool ok = await handler.ExecuteAsync(ability, mixin, invoke.AbilityData ?? Array.Empty<byte>(), Owner, null);
			if (!ok)
			{
				logger.LogWarning($"HandleMixinInvokeAsync<{typeof(TMixin).Name}>: handler returned false");
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"HandleMixinInvokeAsync<{typeof(TMixin).Name}> failed: {ex.Message}");
		}
	}

	/// <summary>
	/// hk4e AbilityComp::addAbility/attachAbility: ability id 0 means
	/// max(applied_ability_map_) + 1, or 1 for an empty map.
	/// </summary>
	protected ActorAbility? AttachActorAbility(
		ConfigAbility config,
		string? overrideName = null,
		uint requestedAbilityId = 0,
		uint overrideNameHash = 0,
		IEnumerable<AbilityScalarValueEntry>? overrideMap = null)
	{
		if (config == null || string.IsNullOrWhiteSpace(config.abilityName))
			return null;

		uint abilityNameHash = GameServer.Ability.Utils.AbilityHash(config.abilityName);
		if (AbilityNameHashSet.Contains(abilityNameHash))
		{
			logger.LogWarning($"Duplicate ability '{config.abilityName}' on entity {Owner._EntityId}");
			return null;
		}

		uint abilityId = requestedAbilityId;
		if (abilityId == 0)
			abilityId = AppliedAbilityMap.Count == 0 ? 1u : checked(AppliedAbilityMap.Keys.Last() + 1u);

		if (AppliedAbilityMap.ContainsKey(abilityId))
		{
			logger.LogWarning($"Duplicate instanced ability id {abilityId} on entity {Owner._EntityId}");
			return null;
		}

		var actorAbility = new ActorAbility(abilityId, Owner, config, overrideName, overrideNameHash);
		if (overrideMap != null)
			actorAbility.LoadOverrideMap(overrideMap);

		AppliedAbilityMap.Add(abilityId, actorAbility);
		AbilityNameHashSet.Add(abilityNameHash);
		InstancedAbilityHashMap[abilityId] = abilityNameHash;
		ConfigAbilityHashMap[abilityNameHash] = config;
		return actorAbility;
	}

	protected bool TryFindActorAbility(uint abilityId, out ActorAbility actorAbility)
	{
		if (abilityId != 0 && AppliedAbilityMap.TryGetValue(abilityId, out ActorAbility? found))
		{
			actorAbility = found;
			return true;
		}

		actorAbility = null!;
		return false;
	}

	/// <summary>
	/// Resolves the ConfigAbility (and optionally the modifier controller)
	/// from authoritative runtime ActorAbility/ActorModifier identity.
	/// </summary>
	/// <param name="invoke">Incoming ability invoke entry.</param>
	/// <param name="ability">Resolved ability config when true is returned.</param>
	/// <param name="modifierController">Resolved modifier controller, if any.</param>
	/// <returns>True if an ability config could be resolved; otherwise false.</returns>
	protected virtual bool TryResolveAbilityForInvoke(
		AbilityInvokeEntry invoke,
		out ConfigAbility ability,
		out ActorModifier? modifierController)
	{
		ability = null!;
		modifierController = null;

		// hk4e serverCommonInvokeHandler resolves an existing modifier first and
		// obtains its parent ActorAbility. applied_modifier_id is a 1-based slot id.
		if (invoke.Head.InstancedModifierId != 0)
		{
			ActorModifier? actorModifier = FindAppliedModifier(invoke.Head.InstancedModifierId);
			if (actorModifier == null)
			{
				logger.LogWarning($"TryResolveAbilityForInvoke: unknown instanced modifier {invoke.Head.InstancedModifierId}.");
				return false;
			}

			modifierController = actorModifier;
			ability = actorModifier.ParentAbility.Config;
			return true;
		}

		if (!TryFindActorAbility(invoke.Head.InstancedAbilityId, out ActorAbility actorAbility))
		{
			logger.LogWarning($"TryResolveAbilityForInvoke: unknown instanced ability {invoke.Head.InstancedAbilityId}.");
			return false;
		}

		ability = actorAbility.Config;
		return true;
	}

	protected virtual void ProcessModifierAction(AbilityInvokeEntry invoke, AbilityMetaModifierChange? modifierChange)
	{
		if (modifierChange == null)
		{
			logger.LogWarning("ProcessModifierAction called with null modifierChange");
			return;
		}

		try
		{
			switch (modifierChange.Action)
			{
				case ModifierAction.Added:
					ProcessAddModifier(invoke, modifierChange);
					break;

				case ModifierAction.Removed:
					ProcessRemoveModifier(invoke, modifierChange);
					break;

				default:
					// hk4e metaHandlerModifierChange only handles action 0 (add)
					// and action 1 (remove); all other enum values return silently.
					return;
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Error processing modifier action: {ex.Message}");
		}
	}

	protected virtual void ProcessRemoveModifier(AbilityInvokeEntry invoke, AbilityMetaModifierChange modifierChange)
	{
		uint instancedModifierId = invoke.Head.InstancedModifierId;
		if (instancedModifierId == 0)
		{
			logger.LogWarning("AbilityMetaModifierChange remove has invalid instancedModifierId=0");
			return;
		}

		uint modifierIndex = instancedModifierId - 1;
		if (!RemoveModifierOnIndex(modifierIndex))
			logger.LogWarning($"Tried to remove unknown instanced modifier {instancedModifierId}");
	}


	/// <summary>
	/// Process adding a modifier to an entity
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual void ProcessAddModifier(AbilityInvokeEntry invoke, AbilityMetaModifierChange modifierChange)
	{
		uint instancedAbilityId = invoke.Head.InstancedAbilityId;
		uint instancedModifierId = invoke.Head.InstancedModifierId;
		if (instancedModifierId == 0)
		{
			logger.LogWarning($"AbilityMetaModifierChange has invalid instancedModifierId=0 (localId={modifierChange.ModifierLocalId})");
			return;
		}

		// hk4e uses head.target_id only to choose the AbilityComp from which the
		// parent ActorAbility is resolved. The new ActorModifier itself always
		// belongs to *this* AbilityComp/applied_modifier_vec_. This distinction is
		// observable: later remove invokes address this manager's modifier slots.
		BaseAbilityManager abilityLookupManager = this;
		if (invoke.Head.TargetId != 0)
		{
			Scene? scene = Owner.session.player?.Scene;
			if (scene == null ||
				!scene.TryFindEntity(invoke.Head.TargetId, out Entity targetEntity) ||
				targetEntity.abilityManager == null)
			{
				logger.LogWarning($"AbilityMetaModifierChange cannot resolve target entity {invoke.Head.TargetId}");
				return;
			}

			abilityLookupManager = targetEntity.abilityManager;
			if (!abilityLookupManager._isInitialized)
				abilityLookupManager.Initialize();

			// AbilityComp::metaHandlerModifierChange aborts if lazy target AbilityComp
			// initialization fails; do not continue against a half-built map.
			if (!abilityLookupManager._isInitialized)
			{
				logger.LogWarning($"AbilityMetaModifierChange target ability manager init failed for entity {targetEntity._EntityId}");
				return;
			}
		}

		if (!abilityLookupManager.TryFindActorAbility(instancedAbilityId, out ActorAbility actorAbility))
		{
			logger.LogWarning($"AbilityMetaModifierChange cannot find instanced ability {instancedAbilityId} on entity {abilityLookupManager.Owner._EntityId}");
			return;
		}

		ConfigAbility ability = actorAbility.Config;
		int modifierLocalId = modifierChange.ModifierLocalId;
		if (modifierLocalId < 0 || ability.modifierIDMap == null || modifierLocalId >= ability.modifierIDMap.Count)
		{
			logger.LogWarning($"AbilityMetaModifierChange invalid modifier local_id={modifierLocalId} for abilityId={instancedAbilityId} ability={ability.abilityName} modifierCount={ability.modifierIDMap?.Count ?? 0}");
			return;
		}

		uint modifierIndex = instancedModifierId - 1;
		if (modifierIndex < AppliedModifierVec.Count && AppliedModifierVec[(int)modifierIndex] != null)
		{
			AppliedModifierVec.Insert((int)modifierIndex, null);
			ResetAppliedModifierIds(modifierIndex);
		}

		AbilityModifier modifierConfig = ability.modifierIDMap[modifierLocalId];
		ActorModifier actorModifier = AddModifierOnIndex(actorAbility, modifierConfig, modifierIndex);
		actorModifier.AttachedModifierOwnerEntityId = modifierChange.AttachedInstancedModifier?.OwnerEntityId ?? 0;
		actorModifier.AttachedModifierId = modifierChange.AttachedInstancedModifier?.InstancedModifierId ?? 0;
		actorModifier.IsMuteRemote = modifierChange.IsMuteRemote;
		actorModifier.ApplyEntityId = modifierChange.ApplyEntityId;
		actorModifier.IsAttachedParentAbility = modifierChange.IsAttachedParentAbility;
		SyncAttachedModifier(actorModifier);

		logger.LogInfo($"Added modifier: Ability={ability.abilityName} AbilityId={instancedAbilityId} ModifierLocalId={modifierLocalId} InstancedModifierId={actorModifier.ModifierId} AbilityEntity={abilityLookupManager.Owner._EntityId} ModifierOwnerEntity={Owner._EntityId}", false);
	}


	/// <summary>
	/// Handles AbilityMetaSetModifierApplyEntity by retargeting an existing
	/// instanced modifier to a new apply_entity_id, mirroring hk4e's
	/// AbilityComp::metaHandleSetModifierApplyEntityId behavior.
	/// </summary>
	protected virtual void HandleSetModifierApplyEntity(AbilityInvokeEntry invoke, AbilityMetaSetModifierApplyEntityId? meta)
	{
		if (meta == null)
		{
			logger.LogWarning("HandleSetModifierApplyEntity called with null meta");
			return;
		}

		uint instancedModifierId = invoke.Head.InstancedModifierId;
		if (instancedModifierId == 0)
		{
			logger.LogWarning("HandleSetModifierApplyEntity: instancedModifierId is 0");
			return;
		}

		ActorModifier? actorModifier = FindAppliedModifier(instancedModifierId);
		if (actorModifier == null)
		{
			logger.LogWarning($"HandleSetModifierApplyEntity: unknown instancedModifierId {instancedModifierId}");
			return;
		}

		actorModifier.ApplyEntityId = meta.ApplyEntityId;
		logger.LogInfo($"HandleSetModifierApplyEntity: retargeted modifier {instancedModifierId} to ApplyEntityId={meta.ApplyEntityId}");
	}

	protected ActorModifier? FindAppliedModifier(uint appliedModifierId)
	{
		if (appliedModifierId == 0)
			return null;
		uint modifierIndex = appliedModifierId - 1;
		return modifierIndex < AppliedModifierVec.Count ? AppliedModifierVec[(int)modifierIndex] : null;
	}

	protected ActorModifier AddModifierOnIndex(ActorAbility ability, AbilityModifier configModifier, uint modifierIndex)
	{
		while (AppliedModifierVec.Count <= modifierIndex)
			AppliedModifierVec.Add(null);

		var modifier = new ActorModifier(ability, Owner, configModifier)
		{
			ModifierId = modifierIndex + 1
		};
		AppliedModifierVec[(int)modifierIndex] = modifier;
		return modifier;
	}

	protected bool RemoveModifierOnIndex(uint modifierIndex)
	{
		if (modifierIndex >= AppliedModifierVec.Count)
			return false;
		ActorModifier? modifier = AppliedModifierVec[(int)modifierIndex];
		if (modifier == null)
			return false;
		modifier.DetachRuntimeLinks();
		AppliedModifierVec[(int)modifierIndex] = null;
		return true;
	}

	protected void ResetAppliedModifierIds(uint start)
	{
		for (uint i = start; i < AppliedModifierVec.Count; i++)
		{
			ActorModifier? modifier = AppliedModifierVec[(int)i];
			if (modifier == null)
				continue;
			modifier.ModifierId = i + 1;
			foreach (ActorModifier attached in modifier.AttachedModifiers.ToArray())
				attached.AttachedModifierId = modifier.ModifierId;
		}
	}

	protected void SyncAttachedModifier(ActorModifier modifier)
	{
		if (modifier.AttachedModifierId == 0)
			return;

		BaseAbilityManager? parentManager = this;
		if (modifier.AttachedModifierOwnerEntityId != 0)
		{
			var entityManager = Owner.session.player.Scene.EntityManager;
			if (!entityManager.TryGet(modifier.AttachedModifierOwnerEntityId, out Entity parentEntity) || parentEntity?.abilityManager == null)
				parentManager = null;
			else
				parentManager = parentEntity.abilityManager;
		}

		ActorModifier? parentModifier = parentManager?.FindAppliedModifier(modifier.AttachedModifierId);
		modifier.AttachToModifier(parentModifier);
	}

	protected void AppendAppliedModifiers(Protocol.AbilitySyncStateInfo syncInfo)
	{
		foreach (ActorModifier? actorModifier in AppliedModifierVec)
		{
			if (actorModifier != null)
				syncInfo.AppliedModifiers.Add(actorModifier.ToProtocol());
		}
	}

	protected virtual void ReInitOverrideMap(uint abilityNameHash, AbilityMetaReInitOverrideMap? overrideMap)
	{
		foreach (var entry in overrideMap.OverrideMaps)
		{
			if (entry.ValueType != AbilityScalarType.AbilityScalarTypeFloat)
			{
				logger.LogWarning($"Unhandled value type {entry.ValueType} in override map for ability hash {abilityNameHash}");
				continue;
			}
			try
			{
				AbilitySpecialOverrideMap[abilityNameHash][entry.Key.Hash] = entry.FloatValue;
			}
			catch
			{
				AbilitySpecialOverrideMap[abilityNameHash] = new();
				AbilitySpecialOverrideMap[abilityNameHash][entry.Key.Hash] = entry.FloatValue;
			}
		}
	}

	public void AddAbilityToEntity(Entity entity, ConfigAbility abilityData)
	{
		var ability = new AbilityInstance(abilityData, entity, entity.session.player);
		entity.InstancedAbilities.Add(ability); // This is in order
	}

	protected virtual void AddAbility(AbilityAppliedAbility ability)
	{
		if (ability == null || ability.AbilityName == null)
		{
			logger.LogWarning("AddAbility: missing ability payload/name.");
			return;
		}

		// hk4e resolves ConfigAbility from ability_name. ability_override is a
		// separate runtime identity and must never replace the base config key.
		string? baseName = ability.AbilityName.Str;
		uint baseHash = ability.AbilityName.Hash != 0
			? ability.AbilityName.Hash
			: (!string.IsNullOrEmpty(baseName) ? GameServer.Ability.Utils.AbilityHash(baseName) : 0u);
		if (baseHash == 0)
		{
			logger.LogWarning("AddAbility: unable to resolve base ability name/hash.");
			return;
		}

		if (!ConfigAbilityHashMap.TryGetValue(baseHash, out ConfigAbility? config) || config == null)
		{
			if (MainApp.resourceManager.ConfigAbilityHashMap == null ||
				!MainApp.resourceManager.ConfigAbilityHashMap.TryGetValue(baseHash, out config) ||
				config == null)
			{
				logger.LogWarning($"AddAbility: config not found for base ability hash {baseHash}");
				return;
			}
		}

		string? overrideName = ability.AbilityOverride?.Str;
		uint overrideHash = ability.AbilityOverride?.Hash ?? 0u;
		ActorAbility? actorAbility = AttachActorAbility(
			config,
			overrideName,
			ability.InstancedAbilityId,
			overrideHash,
			ability.OverrideMaps);
		if (actorAbility == null)
			return;

		// Compatibility projection for float-only callers. Authoritative typed
		// values live in ActorAbility.OverrideMap.
		if (!AbilitySpecialOverrideMap.TryGetValue(baseHash, out var floatMap))
		{
			floatMap = new Dictionary<uint, float>();
			AbilitySpecialOverrideMap[baseHash] = floatMap;
		}

		foreach (var kvp in actorAbility.OverrideMap)
		{
			if (kvp.Value.Kind == AbilityScalarValueKind.Float)
				floatMap[unchecked((uint)kvp.Key)] = kvp.Value.FloatValue;
		}
	}

}

public class ActiveModifierInfo
{
	public int LocalId { get; set; }
	public uint? InstancedModifierId { get; set; }
	public uint ApplyEntityId { get; set; }
	public uint OwnerEntityId { get; set; }
	public uint ParentAbilityNameHash { get; set; }
	public DateTime AppliedTime { get; set; }
	public List<ModifierProperty> Properties { get; set; } = new();

	public ActiveModifierInfo(int localId, uint applyEntityId, uint ownerEntityId, uint parentAbilityNameHash)
	{
		LocalId = localId;
		ApplyEntityId = applyEntityId;
		OwnerEntityId = ownerEntityId;
		ParentAbilityNameHash = parentAbilityNameHash;
		AppliedTime = DateTime.UtcNow;
	}
}