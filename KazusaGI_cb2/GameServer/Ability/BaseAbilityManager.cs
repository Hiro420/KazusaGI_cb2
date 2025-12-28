using Google.Protobuf;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;
using ProtoBuf;

namespace KazusaGI_cb2.GameServer.Systems.Ability;

public abstract class BaseAbilityManager
{
	protected static Logger logger = new("AbilityManager");
	protected readonly Entity Owner;
	// instancedAbilityId -> abilityNameHash (filled from network "applied ability" data)
	protected readonly Dictionary<uint, uint> InstancedAbilityHashMap = new();
	public abstract SortedDictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } // <abilityNameHash, configAbility>
	public readonly Dictionary<uint, Dictionary<uint, float>> AbilitySpecialOverrideMap = new(); // <abilityNameHash, <abilitySpecialNameHash, value>>
	public abstract Dictionary<string, Dictionary<string, float>?>? AbilitySpecials { get; }// <abilityName, <abilitySpecial, value>>
	public abstract HashSet<string> ActiveDynamicAbilities { get; }
	public abstract Dictionary<string, HashSet<string>> UnlockedTalentParams { get; }
	protected Dictionary<uint, string> AbilitySpecialHashMap = new(); // <hash, abilitySpecialName>

	protected Dictionary<uint, float> GlobalValueHashMap = new(); // <hash, value> TODO map the hashes to variable names
	protected Dictionary<int, ActiveModifierInfo> ActiveModifiers = new(); // <modifierLocalId, modifierInfo>

	// <instancedModifierId, AbilityModifierController>
	protected readonly Dictionary<uint, AbilityModifierController> InstancedModifierMap = new();
	protected BaseAbilityManager(Entity owner)
	{
		Owner = owner;
	}

	public virtual void Initialize()
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

		// Ensure all config abilities used by this manager have their
		// LocalIdToInvocationMap / ModifierList initialized, mirroring
		// hk4e's creation of ConfigAbilityImpl & invoke_site_vec.
		foreach (var kvp in ConfigAbilityHashMap)
		{
			var configAbility = kvp.Value;
			if (configAbility == null)
				continue;

			// If initialization has not run (maps still null), run it now.
			if (configAbility.LocalIdToInvocationMap == null || configAbility.ModifierList == null)
			{
				try
				{
					configAbility.Initialize().GetAwaiter().GetResult();
				}
				catch (Exception ex)
				{
					logger.LogError($"Failed to initialize ConfigAbility '{configAbility.abilityName}' for hash {kvp.Key}: {ex.Message}");
				}
			}
		}
	}
	public virtual async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		MemoryStream data = new MemoryStream(invoke.AbilityData);

		if (invoke.Head.LocalId != 255) // INVOCATION_META_LOCALID = 255
		{

			logger.LogInfo($"Server-sided ability invoke: LocalId={invoke.Head.LocalId}, " +
				$"ArgumentType={invoke.ArgumentType}, EntityId={invoke.EntityId}, TargetId={invoke.Head.TargetId}");

			// Mirror hk4e's serverCommonInvokeHandler/commonInvokeEntryDispatch:
			// resolve (ability, modifier) first, then dispatch by LocalId.
			//
			// Primary path: use the bit-packed LocalIdToInvocationMap that
			// is built by LocalIdGenerator / ConfigAbility. This matches
			// hk4e where the client sends packed ids.
			//
			// Fallback: for older/partial configs where only InvokeSiteList
			// was relied on, treat LocalId as a simple index when in range.
			if (!TryResolveAbilityForInvoke(invoke, out var ability, out var modifierController))
			{
				return;
			}
			int localId = invoke.Head.LocalId;
			IInvocation? invocation = null;
			bool resolved = false;
			// 1) Try packed-id lookup first.
			if (ability.LocalIdToInvocationMap != null &&
				ability.LocalIdToInvocationMap.TryGetValue((uint)localId, out var mapped))
			{
				invocation = mapped;
				resolved = true;
			}
			// 2) Fallback: interpret LocalId as a simple index.
			else if (ability.InvokeSiteList != null && localId >= 0 && localId < ability.InvokeSiteList.Count)
			{
				invocation = ability.InvokeSiteList[localId];
				resolved = true;
				logger.LogInfo($"HandleAbilityInvokeAsync: falling back to InvokeSiteList index for localId={localId} on ability {ability.abilityName}");
			}
			if (!resolved || invocation == null)
			{
				logger.LogWarning($"Invalid invoke-site localId={localId} for ability {ability.abilityName} (instancedAbilityId={invoke.Head.InstancedAbilityId}, invokeSiteCount={ability.InvokeSiteList?.Count ?? 0})");
				ability.DebugAbility(logger);
				return;
			}

			logger.LogSuccess($"Invoking ability: {ability.abilityName}, localId: {localId} | {invocation.GetType().Name}");
			Entity? entity2invoke = null;
			EntityManager entityManager = Owner.session.player.Scene.EntityManager;

			if (invoke.EntityId != 0)
				entityManager.TryGet(invoke.EntityId, out entity2invoke);
			if (entity2invoke == null)
				entity2invoke = Owner;

			await invocation.Invoke(ability.abilityName, entity2invoke);

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
		if (!TryResolveAbilityForInvoke(invoke, out ConfigAbility ability, out AbilityModifierController? _))
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
	/// Resolves the ConfigAbility (and optionally the modifier controller)
	/// for a given invoke entry, mirroring hk4e's serverCommonInvokeHandler
	/// resolution order: prefer modifier-based resolution, then fall back
	/// to the applied ability via InstancedAbilityHashMap.
	/// </summary>
	/// <param name="invoke">Incoming ability invoke entry.</param>
	/// <param name="ability">Resolved ability config when true is returned.</param>
	/// <param name="modifierController">Resolved modifier controller, if any.</param>
	/// <returns>True if an ability config could be resolved; otherwise false.</returns>
	protected virtual bool TryResolveAbilityForInvoke(
		AbilityInvokeEntry invoke,
		out ConfigAbility ability,
		out AbilityModifierController? modifierController)
	{
		ability = null!;
		modifierController = null;

		// 1) Prefer resolving via modifier id, like hk4e's logic which
		// first checks the modifier context to find the owning ability.
		if (invoke.Head.InstancedModifierId != 0 &&
			InstancedModifierMap.TryGetValue(invoke.Head.InstancedModifierId, out var controller))
		{
			modifierController = controller;
			ability = controller.AbilityConfig;
			if (ability == null)
			{
				logger.LogWarning($"TryResolveAbilityForInvoke: modifier {invoke.Head.InstancedModifierId} has null AbilityConfig.");
				return false;
			}
			return true;
		}

		// 2) Fall back to instanced ability id mapping.
		uint instancedAbilityId = invoke.Head.InstancedAbilityId;
		if (instancedAbilityId == 0)
		{
			logger.LogWarning("TryResolveAbilityForInvoke: instancedAbilityId is 0 and no modifier context was found.");
			return false;
		}

		if (!InstancedAbilityHashMap.TryGetValue(instancedAbilityId, out uint abilityHash))
		{
			logger.LogWarning($"TryResolveAbilityForInvoke: no ability hash for instancedAbilityId {instancedAbilityId}.");
			return false;
		}

		// Try to get the config from this manager first.
		if (!ConfigAbilityHashMap.TryGetValue(abilityHash, out var configAbility) || configAbility == null)
		{
			// Fallback: pull from global ConfigAbilityHashMap if available,
			// similar to how AddAbility and ProcessAddModifier behave.
			if (MainApp.resourceManager.ConfigAbilityHashMap == null ||
				!MainApp.resourceManager.ConfigAbilityHashMap.TryGetValue(abilityHash, out configAbility) ||
				configAbility == null)
			{
				logger.LogWarning($"TryResolveAbilityForInvoke: config not found for ability hash {abilityHash} (instancedAbilityId={instancedAbilityId}).");
				return false;
			}

			// Ensure invoke/mixin/modifier indices are ready, then bind into this manager.
			try
			{
				if (configAbility.LocalIdToInvocationMap == null || configAbility.ModifierList == null)
				{
					configAbility.Initialize().GetAwaiter().GetResult();
				}
				ConfigAbilityHashMap[abilityHash] = configAbility;
				logger.LogInfo($"TryResolveAbilityForInvoke: bound global ConfigAbility '{configAbility.abilityName}' to hash {abilityHash} for instancedAbilityId {instancedAbilityId}.");
			}
			catch (Exception ex)
			{
				logger.LogError($"TryResolveAbilityForInvoke: failed to initialize/bind global ConfigAbility for hash {abilityHash}: {ex.Message}");
				return false;
			}
		}

		ability = configAbility;
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
					logger.LogWarning($"Unknown modifier action: {modifierChange.Action}");
					break;
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

		if (InstancedModifierMap.Remove(instancedModifierId))
		{
			logger.LogInfo($"Removed instanced modifier {instancedModifierId}");
		}
		else
		{
			logger.LogWarning($"Tried to remove unknown instanced modifier {instancedModifierId}");
		}

		ActiveModifiers.Remove(modifierChange.ModifierLocalId);
	}


	/// <summary>
	/// Process adding a modifier to an entity
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual void ProcessAddModifier(AbilityInvokeEntry invoke, AbilityMetaModifierChange modifierChange)
	{
		logger.LogInfo($"Adding modifier: LocalId={modifierChange.ModifierLocalId}, " +
			$"ConfigLocalId={invoke.Head.ModifierConfigLocalId}, " +
			$"ParentAbility={modifierChange.ParentAbilityName?.Hash:X}");

		//logger.LogWarning("Modifier Change Data:");
		//logger.LogWarning(Newtonsoft.Json.JsonConvert.SerializeObject(modifierChange, Newtonsoft.Json.Formatting.Indented));
		//logger.LogWarning("Full Invoke Data:");
		//logger.LogWarning(Newtonsoft.Json.JsonConvert.SerializeObject(invoke, Newtonsoft.Json.Formatting.Indented));

		try
		{
			// figure out who the modifier is applied to
			uint targetEntityId = modifierChange.ApplyEntityId != 0
				? modifierChange.ApplyEntityId
				: Owner._EntityId;

			uint instancedAbilityId = invoke.Head.InstancedAbilityId;
			uint instancedModifierId = invoke.Head.InstancedModifierId;
			if (instancedModifierId == 0)
			{
				logger.LogWarning($"AbilityMetaModifierChange has invalid instancedModifierId=0 (localId={modifierChange.ModifierLocalId})");
				return;
			}

			if (!InstancedAbilityHashMap.TryGetValue(instancedAbilityId, out uint abilityHash))
			{
				// hk4e: when instancedAbilityId is not cached, find the first
				// ability in this manager that has a modifier with the given
				// config local id and bind it.
				uint configLocalId = (uint)invoke.Head.ModifierConfigLocalId;
				ConfigAbility? matchedAbility = null;
				uint matchedHash = 0;

				foreach (var kv in ConfigAbilityHashMap)
				{
					if (kv.Value?.ModifierList != null &&
						kv.Value.ModifierList.ContainsKey(configLocalId))
					{
						matchedHash = kv.Key;
						matchedAbility = kv.Value;
						break;
					}
				}

				if (matchedHash == 0)
				{
					logger.LogWarning($"No ability found with ModifierConfigLocalId={configLocalId} for instancedAbilityId {instancedAbilityId}");
					return;
				}

				abilityHash = matchedHash;
				InstancedAbilityHashMap[instancedAbilityId] = abilityHash;
				if (!ConfigAbilityHashMap.TryGetValue(abilityHash, out ConfigAbility? ability))
				{
					var sceneManager = Owner.session.player.Scene.EntityManager;
					logger.LogWarning($"Missing ability config for ability hash {abilityHash} | entity {targetEntityId} of type {sceneManager.Entities[targetEntityId].GetType().Name}");
					return;
				}

				// hk4e: modifier config is addressed by modifier_config_local_id
				AbilityModifier? modifierConfig = null!;
				if (ability.ModifierList == null ||
					!ability.ModifierList.TryGetValue(configLocalId, out modifierConfig))
				{
					logger.LogWarning($"No modifier config for configLocalId={configLocalId} in ability {ability.abilityName}");
					return;
				}

				// create the controller ("AbilityModifierController" like in GC)
				var controller = new AbilityModifierController(
					instancedAbilityId,
					instancedModifierId,
					ability,
					modifierConfig,
					modifierChange);

				// add to InstancedModifierMap at index = instancedModifierId (12)
				if (InstancedModifierMap.ContainsKey(instancedModifierId))
				{
					logger.LogWarning(
						$"InstancedModifierId {instancedModifierId} already exists on add. " +
						$"Game should have sent REMOVE before ADD – check your logic.");
					// decide whether to overwrite or bail; GC usually treats this as an error
					InstancedModifierMap[instancedModifierId] = controller;
				}
				else
				{
					InstancedModifierMap.Add(instancedModifierId, controller);
				}

				var modifierInfo = new ActiveModifierInfo(
					modifierChange.ModifierLocalId,
					targetEntityId,
					modifierChange.AttachedInstancedModifier?.OwnerEntityId ?? Owner._EntityId,
					abilityHash)
				{
					Properties = modifierChange.Properties.ToList(),
					InstancedModifierId = instancedModifierId
				};

				ActiveModifiers[modifierChange.ModifierLocalId] = modifierInfo;

				logger.LogInfo($"Successfully applied and tracked modifier: " +
					$"ModifierLocalId={modifierChange.ModifierLocalId}, InstancedModifierId={instancedModifierId}, " +
					$"TargetEntity={targetEntityId}, Properties={modifierChange.Properties.Count}", false);
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to apply modifier: {ex.Message}");
		}
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

		if (!InstancedModifierMap.TryGetValue(instancedModifierId, out var controller))
		{
			logger.LogWarning($"HandleSetModifierApplyEntity: unknown instancedModifierId {instancedModifierId}");
			return;
		}

		uint newApplyEntityId = meta.ApplyEntityId;
		controller.MetaData.ApplyEntityId = newApplyEntityId;

		// Keep ActiveModifiers in sync: find the entry bound to this instanced modifier
		// and retarget its ApplyEntityId.
		foreach (var kv in ActiveModifiers)
		{
			var info = kv.Value;
			if (info.InstancedModifierId == instancedModifierId)
			{
				info.ApplyEntityId = newApplyEntityId;
				break;
			}
		}

		logger.LogInfo($"HandleSetModifierApplyEntity: retargeted modifier {instancedModifierId} to ApplyEntityId={newApplyEntityId}");
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
		// Resolve the effective ability name/hash, preferring override
		// strings/hashes, mirroring hk4e's AbilityAppliedAbility usage.
		uint hash = 0;
		string? abilityNameStr = null;
		string? overrideNameStr = null;

		if (ability.AbilityOverride != null)
		{
			overrideNameStr = ability.AbilityOverride.Str;
			if (ability.AbilityOverride.Hash != 0)
				hash = ability.AbilityOverride.Hash;
			else if (!string.IsNullOrEmpty(overrideNameStr))
				hash = GameServer.Ability.Utils.AbilityHash(overrideNameStr);
		}

		if (hash == 0 && ability.AbilityName != null)
		{
			abilityNameStr = ability.AbilityName.Str;
			if (ability.AbilityName.Hash != 0)
				hash = ability.AbilityName.Hash;
			else if (!string.IsNullOrEmpty(abilityNameStr))
				hash = GameServer.Ability.Utils.AbilityHash(abilityNameStr);
		}

		if (hash == 0)
		{
			logger.LogWarning("AddAbility: unable to resolve ability hash from AbilityAppliedAbility (both override and base empty).");
			return;
		}

		uint instancedId = ability.InstancedAbilityId;
		InstancedAbilityHashMap[instancedId] = hash;

		// Ensure this manager has a config entry for the ability hash.
		// If it's not in the per-entity map yet, fall back to the global
		// ConfigAbilityHashMap from ResourceManager (mirrors hk4e where
		// applied abilities reference already-loaded configs).
		if (!ConfigAbilityHashMap.ContainsKey(hash))
		{
			if (MainApp.resourceManager.ConfigAbilityHashMap != null &&
				MainApp.resourceManager.ConfigAbilityHashMap.TryGetValue(hash, out var globalConfig) &&
				globalConfig != null)
			{
				try
				{
					// Make sure invoke/mixin/modifier indices are ready for this ability.
					if (globalConfig.LocalIdToInvocationMap == null || globalConfig.ModifierList == null)
					{
						globalConfig.Initialize().GetAwaiter().GetResult();
					}

					ConfigAbilityHashMap[hash] = globalConfig;
					logger.LogInfo($"AddAbility: bound global ConfigAbility '{globalConfig.abilityName}' to hash {hash} for instancedId {instancedId}.");
				}
				catch (Exception ex)
				{
					logger.LogError($"AddAbility: failed to initialize/bind global ConfigAbility for hash {hash}: {ex.Message}");
				}
			}
			else
			{
				logger.LogWarning($"AddAbility: config not found for ability hash {hash} (override='{overrideNameStr}', base='{abilityNameStr}')");
			}
		}

		if (ability.OverrideMaps.Any())
		{
			if (!AbilitySpecialOverrideMap.TryGetValue(hash, out var specials))
			{
				specials = new Dictionary<uint, float>();
				AbilitySpecialOverrideMap[hash] = specials;
			}

			foreach (var entry in ability.OverrideMaps)
			{
				switch (entry.ValueType)
				{
					case AbilityScalarType.AbilityScalarTypeFloat:
						specials[entry.Key.Hash] = entry.FloatValue;
						break;
					default:
						logger.LogWarning($"Unhandled value type {entry.ValueType} in AddAbility override for ability hash {hash}");
						break;
				}
			}
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