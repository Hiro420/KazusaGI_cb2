using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using ProtoBuf;

namespace KazusaGI_cb2.GameServer.Ability;

/// <summary>
/// Tracks information about a modifier currently applied to an entity.
/// Mirrors the bookkeeping Grasscutter does for modifier state.
/// </summary>
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

/// <summary>
/// Central ability manager for an entity.
///
/// This plays a similar role to Grasscutter's AbilityManager: it owns
/// mixin/action handler registration, dispatches AbilityInvokeEntry messages,
/// tracks modifier state, and maintains ability override/global values.
///
/// Concrete subclasses (avatar, monster, gadget) provide their own
/// ConfigAbility collections and specials, then call Initialize() to wire
/// hashes and override maps.
/// </summary>
public abstract class BaseAbilityManager
{
	public static readonly Logger logger = new("AbilityManager");
	protected readonly Entity Owner;
	
	/// <summary>
	/// Static registry of mixin handlers, similar to Grasscutter's mixinHandlers.
	/// </summary>
	private static readonly Dictionary<Type, AbilityMixinHandler> mixinHandlers = new();
	
	/// <summary>
	/// Static constructor: scan assembly once and register all mixin handlers.
	/// </summary>
	static BaseAbilityManager()
	{
		RegisterMixinHandlers();
	}
	public Dictionary<uint, uint> InstanceToAbilityHashMap = new(); // <instancedAbilityId, abilityNameHash>
	public abstract Dictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } // <abilityNameHash, configAbility>
	public readonly Dictionary<uint, Dictionary<uint, float>> AbilitySpecialOverrideMap = new(); // <abilityNameHash, <abilitySpecialNameHash, value>>
	public abstract Dictionary<string, Dictionary<string, float>?>? AbilitySpecials { get; }// <abilityName, <abilitySpecial, value>>
	public abstract HashSet<string> ActiveDynamicAbilities { get; }
	public abstract Dictionary<string, HashSet<string>> UnlockedTalentParams { get; }
	protected Dictionary<uint, string> AbilitySpecialHashMap = new(); // <hash, abilitySpecialName>

	public Dictionary<AbilityString, AbilityScalarValueEntry> GlobalValueHashMap = new(); // <hash, value> TODO map the hashes to variable names
	
	/// <summary>
	/// Tracks active modifiers by their local ID for easy removal
	/// </summary>
	protected Dictionary<int, ActiveModifierInfo> ActiveModifiers = new(); // <modifierLocalId, modifierInfo>
	
	/// <summary>
	/// Tracks active modifiers by entity ID for entity cleanup
	/// </summary>
	protected Dictionary<uint, HashSet<int>> EntityModifiers = new(); // <entityId, set of modifierLocalIds>
	
	protected BaseAbilityManager(Entity owner)
	{
		Owner = owner;
	}

	// Dunno how to handle it
	public bool IsInited { get; set; } = false;

    /// <summary>
    /// Register all mixin handlers by scanning assemblies for classes with AbilityMixinAttribute
    /// </summary>
    private static void RegisterMixinHandlers()
	{
		try
		{
			var assembly = Assembly.GetExecutingAssembly();
			var handlerTypes = assembly.GetTypes()
				.Where(t => t.IsSubclassOf(typeof(AbilityMixinHandler)) && !t.IsAbstract);

			foreach (var handlerType in handlerTypes)
			{
				var attribute = handlerType.GetCustomAttribute<AbilityMixinAttribute>();
				if (attribute != null)
				{
					var handler = (AbilityMixinHandler)Activator.CreateInstance(handlerType)!;
					mixinHandlers[attribute.MixinType] = handler;
					logger.LogInfo($"Registered mixin handler: {handlerType.Name} for mixin type: {attribute.MixinType.Name}");
				}
			}
			
			logger.LogInfo($"Successfully registered {mixinHandlers.Count} mixin handlers");
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to register mixin handlers: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Execute a mixin with the specified parameters, similar to GC's executeMixin method
	/// </summary>
	/// <param name="ability">The ability containing the mixin</param>
	/// <param name="mixin">The mixin to execute</param>
	/// <param name="abilityData">Additional ability data</param>
	/// <param name="source">Source entity</param>
	/// <param name="target">Target entity (optional)</param>
	public virtual async Task ExecuteMixinAsync(
		ConfigAbility ability,
		BaseAbilityMixin mixin,
		byte[] abilityData,
		Entity source,
		Entity? target = null)
	{
		var mixinType = mixin.GetType();
		if (!mixinHandlers.TryGetValue(mixinType, out var handler))
		{
			logger.LogWarning($"No handler registered for mixin type: {mixinType.Name}");
			return;
		}
		
		try
		{
			var result = await handler.ExecuteAsync(ability, mixin, abilityData, source, target);
			if (!result)
			{
				logger.LogWarning($"Mixin handler {handler.GetType().Name} returned false for ability: {ability.abilityName}");
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Error executing mixin {mixinType.Name} for ability {ability.abilityName}: {ex.Message}");
		}
	}

	public virtual void Initialize()
    {
        foreach (var ability in AbilitySpecials)
		{
			uint ablHash = Utils.AbilityHash(ability.Key);
			AbilitySpecialOverrideMap[ablHash] = new();
			if (ability.Value != null)
			{
				foreach (var special in ability.Value)
				{
					AbilitySpecialOverrideMap[ablHash][Utils.AbilityHash(special.Key)] = special.Value;
					AbilitySpecialHashMap[Utils.AbilityHash(special.Key)] = special.Key;
				}
			}
		}

    }


	public virtual async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		ProtoBuf.IExtensible info = new AbilityMetaModifierChange();
		MemoryStream data = new MemoryStream(invoke.AbilityData);

		// First, try to handle server-sided invokes (when LocalId != 255) for specific abilities
		if (invoke.Head.LocalId != 255) // INVOCATION_META_LOCALID = 255
		{
			logger.LogInfo($"Server-sided ability invoke: LocalId={invoke.Head.LocalId}, " +
				$"ArgumentType={invoke.ArgumentType}, EntityId={invoke.EntityId}, TargetId={invoke.Head.TargetId}", false);

			// Try to find the ability and execute mixin/action if found
			ConfigAbility? ability = null;
			if (invoke.Head.InstancedAbilityId == 0)
				return;

			if (invoke.Head.InstancedModifierId != 0)
			{
				/*
					if (head.getInstancedModifierId() != 0
							&& entity.getInstancedModifiers().containsKey(head.getInstancedModifierId())) {
						ability = entity.getInstancedModifiers().get(head.getInstancedModifierId()).getAbility();
					}
				*/

				foreach (var entry in ActiveModifiers)
				{
					Console.WriteLine($"Active Modifier: LocalId={entry.Key}, InstancedModifierId={entry.Value.InstancedModifierId}, ParentAbility={entry.Value.ParentAbilityNameHash}");
                }

				var instModId = invoke.Head.InstancedModifierId;
                ActiveModifiers.TryGetValue((int)instModId, out var activeMod);
                if (activeMod != null)
				{
					logger.LogWarning($"Found active modifier for instancedModifierId: {instModId} -> LocalId={activeMod.LocalId}, ParentAbility={activeMod.ParentAbilityNameHash}");
					if (ConfigAbilityHashMap.TryGetValue(activeMod.ParentAbilityNameHash, out ability))
					{
						logger.LogSuccess($"Resolved ability by instancedModifierId: {instModId} -> {ability.abilityName}", false);
					}
				}
            }

            /*
				if (ability == null
						&& head.getInstancedAbilityId() != 0
						&& (head.getInstancedAbilityId() - 1) < entity.getInstancedAbilities().size()) {
					ability = entity.getInstancedAbilities().get(head.getInstancedAbilityId() - 1);
				}
			*/

			if (ability == null && invoke.Head.InstancedAbilityId != 0)
			{
				var instAblId = invoke.Head.InstancedAbilityId;
				foreach (var entry in InstanceToAbilityHashMap)
				{
					Console.WriteLine($"InstanceToAbilityHashMap: InstancedAbilityId={entry.Key}, AbilityHash={entry.Value}");
                }
                if (InstanceToAbilityHashMap.TryGetValue(instAblId, out uint abilityHash))
				{
					if (ConfigAbilityHashMap.TryGetValue(abilityHash, out ability))
					{
						logger.LogSuccess($"Resolved ability by instancedAbilityId: {instAblId} -> {ability.abilityName}", false);
					}
				}
            }

            if (ability == null)
			{
				logger.LogError($"[AbilityManager] Ability not found: ability {invoke.Head.InstancedAbilityId} modifier {invoke.Head.InstancedModifierId}");
				return;
			}

			// Found specific ability - check for mixins/actions
			if (ability.LocalIdToInvocationMap.TryGetValue((uint)invoke.Head.LocalId, out IInvocation invocation))
            {
                if (invocation is BaseAbilityMixin mixin)
                {
                    await ExecuteMixinAsync(ability, mixin, invoke.AbilityData, Owner);
                    return; // Mixin processed, don't continue to argument type processing
                }
                else
                {
                    await invocation.Invoke(ability.abilityName, Owner);
                    return; // Action processed, don't continue to argument type processing
                }
            }

            // If no specific ability action/mixin was found, continue to process by argument type
            logger.LogInfo($"No specific ability action found, processing by argument type: {invoke.ArgumentType}", false);
		}
		
		switch (invoke.ArgumentType)
		{
			case AbilityInvokeArgument.AbilityNone:
				logger.LogError($"Missing localId: {invoke.Head.LocalId}, ability: {invoke.Head.InstancedAbilityId}");
				info = new AbilityMetaModifierChange(); // just to satisfy the compiler. In this case abilityData is empty anyway.
				break;
			case AbilityInvokeArgument.AbilityMetaModifierChange:
				info = Serializer.Deserialize<AbilityMetaModifierChange>(data);
				var modifierChange = info as AbilityMetaModifierChange;
				logger.LogInfo($"Processing modifier change: Action={modifierChange?.Action}", false);
				ProcessModifierAction(modifierChange);
				break;
			// case AbilityInvokeArgument.AbilityMetaSpecialFloatArgument:
			// 	info = Serializer.Deserialize<AbilityMetaSpecialFloatArgument>(data);
			// 	break;
			case AbilityInvokeArgument.AbilityMetaOverrideParam:
				info = Serializer.Deserialize<AbilityScalarValueEntry>(data);
				var asEntri = info as AbilityScalarValueEntry;
				AbilitySpecialOverrideMap[InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]][asEntri.Key.Hash] = asEntri.FloatValue;
				break;
			case AbilityInvokeArgument.AbilityMetaReinitOverridemap:
				info = Serializer.Deserialize<AbilityMetaReInitOverrideMap>(data);
				ReInitOverrideMap(InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId], info as AbilityMetaReInitOverrideMap);
				break;
            case AbilityInvokeArgument.AbilityMetaGlobalFloatValue:
            {
                info = Serializer.Deserialize<AbilityScalarValueEntry>(data);
                var asEntry = info as AbilityScalarValueEntry;
                if (asEntry == null)
                    break;

				AbilityString? existing = GlobalValueHashMap.Keys.FirstOrDefault(k => k.Hash == asEntry.Key.Hash);
				if (existing != null)
					GlobalValueHashMap.Remove(existing);
				GlobalValueHashMap[asEntry.Key] = asEntry;
                break;
            }
            // case AbilityInvokeArgument.AbilityMetaAddOrGetAbilityAndTrigger:
            // 	info = Serializer.Deserialize<AbilityMetaAddOrGetAbilityAndTrigger>(data);
            // 	break;
            case AbilityInvokeArgument.AbilityMetaAddNewAbility:
				info = Serializer.Deserialize<AbilityMetaAddAbility>(data);
				AddAbility((info as AbilityMetaAddAbility).Ability);
				break;
			case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
				info = Serializer.Deserialize<AbilityMetaModifierDurabilityChange>(data);
				var durabilityChange = info as AbilityMetaModifierDurabilityChange;
				//logger.LogInfo($"Processing modifier durability change: {invoke.Head.InstancedModifierId}", false);
				break;
			case AbilityInvokeArgument.AbilityActionTriggerAbility:
				info = Serializer.Deserialize<AbilityActionTriggerAbility>(data);
				var triggerAction = info as AbilityActionTriggerAbility;
				ProcessTriggerAbilityAction(triggerAction);
				break;
			case AbilityInvokeArgument.AbilityActionGenerateElemBall:
				info = Serializer.Deserialize<AbilityActionGenerateElemBall>(data);
				//Owner.GenerateElemBall((AbilityActionGenerateElemBall)info);
				break;
			// case AbilityInvokeArgument.AbilityMixinWindZone:
			// 	info = Serializer.Deserialize<AbilityMixinWindZone>(data);
			// 	break;
			// case AbilityInvokeArgument.AbilityMixinCostStamina:
			// 	info = Serializer.Deserialize<AbilityMixinCostStamina>(data);
			// 	break;
			// case AbilityInvokeArgument.AbilityMixinGlobalShield:
			// 	info = Serializer.Deserialize<AbilityMixinGlobalShield>(data);
			// 	break;
			// case AbilityInvokeArgument.AbilityMixinWindSeedSpawner:
			// 	info = Serializer.Deserialize<AbilityMixinWindSeedSpawner>(data);
			// 	break;
			case AbilityInvokeArgument.AbilityMetaSetKilledSetate:
				info = Serializer.Deserialize<AbilityMetaSetKilledState>(data);
				AbilityMetaSetKilledState state = (AbilityMetaSetKilledState)info;
				if (state.Killed)
				{
					//Owner.ForceKill();
				}
				break;
			default:
				logger.LogWarning($"Unhandled AbilityInvokeArgument: {invoke.ArgumentType}");
				info = new AbilityMetaModifierChange(); // should not happen, just to satisfy the compiler
				break;
		}

		//logger.LogInfo($"RECV ability invoke: {invoke} {info.GetType()} {Owner._EntityId}", true);
	}
	
	/// <summary>
	/// Process modifier action (add/remove modifiers), similar to GC's modifier handling
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual void ProcessModifierAction(AbilityMetaModifierChange? modifierChange)
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
					ProcessAddModifier(modifierChange);
					break;
					
				case ModifierAction.Removed:
					ProcessRemoveModifier(modifierChange);
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
	
	/// <summary>
	/// Process adding a modifier to an entity
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual void ProcessAddModifier(AbilityMetaModifierChange modifierChange)
	{
		logger.LogInfo($"Adding modifier: LocalId={modifierChange.ModifierLocalId}, " +
			$"ParentAbility={modifierChange.ParentAbilityName?.Hash:X} ");

		// Process modifier properties if any
		if (modifierChange.Properties.Count > 0)
		{
			foreach (var property in modifierChange.Properties)
			{
				//logger.LogInfo($"Modifier property: {property.Key?.Str} = {property.Value}", false);
				
				// Store property values in global value map for later use
				if (property.Key != null)
				{
					var entry = new AbilityScalarValueEntry
					{
						Key = property.Key,
						FloatValue = property.Value,
						ValueType = AbilityScalarType.AbilityScalarTypeFloat
					};
					GlobalValueHashMap[property.Key] = entry;
				}
			}
		}
		
		// Implement actual modifier application to entity
		try
		{
			// 1. Find the target entity by ApplyEntityId (default to owner if 0)
			uint targetEntityId = modifierChange.ApplyEntityId != 0 ? modifierChange.ApplyEntityId : Owner._EntityId;
			
			// 2. Create the modifier tracking info
			var modifierInfo = new ActiveModifierInfo(
				modifierChange.ModifierLocalId, 
				targetEntityId, 
				modifierChange.AttachedInstancedModifier?.OwnerEntityId ?? Owner._EntityId,
				modifierChange.ParentAbilityName?.Hash ?? 0)
			{
				Properties = modifierChange.Properties.ToList()
			};
			
			if (modifierChange.AttachedInstancedModifier != null)
			{
				modifierInfo.InstancedModifierId = modifierChange.AttachedInstancedModifier.InstancedModifierId;
			}
			
			// 3. Track the modifier for later removal
			ActiveModifiers[modifierChange.ModifierLocalId] = modifierInfo;
			
			// Track by entity for cleanup
			if (!EntityModifiers.ContainsKey(targetEntityId))
			{
				EntityModifiers[targetEntityId] = new HashSet<int>();
			}
			EntityModifiers[targetEntityId].Add(modifierChange.ModifierLocalId);

			// add it to InstanceToAbilityHashMap
			if (modifierChange.ParentAbilityName != null)
			{
				uint abilityHash = modifierChange.ParentAbilityName.Hash;
                if (!InstanceToAbilityHashMap.ContainsValue(abilityHash) && modifierInfo.InstancedModifierId.HasValue)
				{
					InstanceToAbilityHashMap[modifierInfo.InstancedModifierId.Value] = abilityHash;
				}
			}

            logger.LogInfo($"Successfully applied and tracked modifier: ModifierLocalId={modifierChange.ModifierLocalId}, " +
				$"TargetEntity={targetEntityId}, Properties={modifierChange.Properties.Count}", false);

		}
        catch (Exception ex)
		{
			logger.LogError($"Failed to apply modifier: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Process removing a modifier from an entity
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual void ProcessRemoveModifier(AbilityMetaModifierChange modifierChange)
	{
		//logger.LogInfo($"Removing modifier: LocalId={modifierChange.ModifierLocalId}, " +
		//	$"ParentAbility={modifierChange.ParentAbilityName?.Str}, " +
		//	$"ApplyEntityId={modifierChange.ApplyEntityId}", false);
		
		// Handle attached modifier removal if present
		if (modifierChange.AttachedInstancedModifier != null)
		{
			var attached = modifierChange.AttachedInstancedModifier;
			//logger.LogInfo($"Removing attached modifier: InstancedId={attached.InstancedModifierId}, " +
			//	$"OwnerEntityId={attached.OwnerEntityId}", false);
		}
		
		// Implement actual modifier removal from entity
		try
		{
			// 1. Find the modifier by LocalId
			if (ActiveModifiers.TryGetValue(modifierChange.ModifierLocalId, out var modifierInfo))
			{
				// 2. Remove from entity tracking
				if (EntityModifiers.TryGetValue(modifierInfo.ApplyEntityId, out var entityMods))
				{
					entityMods.Remove(modifierChange.ModifierLocalId);
					
					// Clean up empty entity entries
					if (entityMods.Count == 0)
					{
						EntityModifiers.Remove(modifierInfo.ApplyEntityId);
					}
				}
				
				// 3. Remove from active modifiers
				ActiveModifiers.Remove(modifierChange.ModifierLocalId);
				
				//logger.LogInfo($"Successfully removed modifier: LocalId={modifierChange.ModifierLocalId}, " +
				//	$"TargetEntity={modifierInfo.ApplyEntityId}, Duration={(DateTime.UtcNow - modifierInfo.AppliedTime).TotalSeconds:F1}s", false);
					
				// 4. Remove modifier effects from entity (implement based on your entity system)
				// Note: This would need to be implemented based on your specific entity system
			}
			else
			{
				//logger.LogWarning($"Modifier not found in active tracking: LocalId={modifierChange.ModifierLocalId}");
				
				// Try to find by InstancedModifierId as fallback
				if (modifierChange.AttachedInstancedModifier != null)
				{
					var instancedId = modifierChange.AttachedInstancedModifier.InstancedModifierId;
					var foundModifier = ActiveModifiers.Values.FirstOrDefault(m => m.InstancedModifierId == instancedId);
					
					if (foundModifier != null)
					{
						// Remove using the found LocalId
						RemoveModifierByLocalId(foundModifier.LocalId);
						//logger.LogInfo($"Removed modifier by InstancedModifierId: {instancedId} -> LocalId={foundModifier.LocalId}", false);
					}
					else
					{
						//logger.LogWarning($"Modifier not found by InstancedModifierId: {instancedId}");
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to remove modifier: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Helper method to remove a modifier by its LocalId
	/// </summary>
	/// <param name="localId">The LocalId of the modifier to remove</param>
	protected virtual bool RemoveModifierByLocalId(int localId)
	{
		if (ActiveModifiers.TryGetValue(localId, out var modifierInfo))
		{
			// Remove from entity tracking
			if (EntityModifiers.TryGetValue(modifierInfo.ApplyEntityId, out var entityMods))
			{
				entityMods.Remove(localId);
				if (entityMods.Count == 0)
				{
					EntityModifiers.Remove(modifierInfo.ApplyEntityId);
				}
			}
			
			// Remove from active modifiers
			ActiveModifiers.Remove(localId);
			return true;
		}
		return false;
	}
	
	/// <summary>
	/// Remove all modifiers from a specific entity (useful for entity cleanup)
	/// </summary>
	/// <param name="entityId">The entity ID to clean up</param>
	public virtual void RemoveAllModifiersFromEntity(uint entityId)
	{
		if (EntityModifiers.TryGetValue(entityId, out var modifierIds))
		{
			var idsToRemove = modifierIds.ToList(); // Copy to avoid modification during iteration
			
			foreach (var modifierId in idsToRemove)
			{
				RemoveModifierByLocalId(modifierId);
			}
			
			logger.LogInfo($"Removed {idsToRemove.Count} modifiers from entity {entityId}", false);
		}
	}
	
	/// <summary>
	/// Get information about active modifiers for debugging
	/// </summary>
	/// <returns>Dictionary of active modifiers with their info</returns>
	public virtual Dictionary<int, ActiveModifierInfo> GetActiveModifiers()
	{
		return new Dictionary<int, ActiveModifierInfo>(ActiveModifiers);
	}
	
	/// <summary>
	/// Get count of active modifiers
	/// </summary>
	/// <returns>Number of active modifiers</returns>
	public virtual int GetActiveModifierCount()
	{
		return ActiveModifiers.Count;
	}
	
	/// <summary>
	/// Get modifiers for a specific entity
	/// </summary>
	/// <param name="entityId">The entity ID</param>
	/// <returns>List of modifier info for the entity</returns>
	public virtual List<ActiveModifierInfo> GetModifiersForEntity(uint entityId)
	{
		if (!EntityModifiers.TryGetValue(entityId, out var modifierIds))
		{
			return new List<ActiveModifierInfo>();
		}
		
		return modifierIds
			.Where(id => ActiveModifiers.ContainsKey(id))
			.Select(id => ActiveModifiers[id])
			.ToList();
	}

	/// <summary>
	/// Process trigger ability action - triggers another ability
	/// </summary>
	/// <param name="triggerAction">The trigger action data</param>
	protected virtual void ProcessTriggerAbilityAction(AbilityActionTriggerAbility? triggerAction)
	{
		if (triggerAction == null)
		{
			logger.LogWarning("ProcessTriggerAbilityAction called with null triggerAction");
			return;
		}
		
		try
		{
			logger.LogInfo($"Triggering ability: OtherId={triggerAction.OtherId}", false);
			
			// Try to find the ability to trigger by OtherId
			ConfigAbility? targetAbility = null;
			if (InstanceToAbilityHashMap.TryGetValue(triggerAction.OtherId, out uint abilityHash)
				&& ConfigAbilityHashMap.TryGetValue(abilityHash, out targetAbility))
			{
				logger.LogInfo($"Found target ability to trigger: {targetAbility.abilityName}", false);
				
				// Implement actual ability triggering by instance ID
				_ = TriggerAbilityByInstance(triggerAction.OtherId, targetAbility);
			}
			else
			{
				// If not found by instance ID, it might be a direct ability hash
				if (ConfigAbilityHashMap.TryGetValue(triggerAction.OtherId, out targetAbility))
				{
					logger.LogInfo($"Found target ability by direct hash: {targetAbility.abilityName}", false);
					
					// Implement direct ability hash triggering
					_ = TriggerAbilityByHash(triggerAction.OtherId, targetAbility);
				}
				else
				{
					logger.LogWarning($"Could not find ability to trigger: OtherId={triggerAction.OtherId}");
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Error processing trigger ability action: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Trigger an ability by its instance ID
	/// </summary>
	/// <param name="instancedAbilityId">The instanced ability ID</param>
	/// <param name="ability">The ability configuration</param>
	protected virtual async Task TriggerAbilityByInstance(uint instancedAbilityId, ConfigAbility ability)
	{
		try
		{
			// Create a new ability invoke entry for the triggered ability
			var triggerInvoke = new AbilityInvokeEntry
			{
				Head = new AbilityInvokeEntryHead
				{
					InstancedAbilityId = instancedAbilityId,
					LocalId = 0, // Set to 0 for triggered abilities unless specific LocalId is needed
					TargetId = Owner._EntityId
				},
				EntityId = Owner._EntityId,
				ArgumentType = AbilityInvokeArgument.AbilityNone, // Default to None for triggered abilities
				AbilityData = Array.Empty<byte>() // Empty data for basic trigger
			};
			
			logger.LogInfo($"Triggering ability by instance: {ability.abilityName} (InstanceId={instancedAbilityId})", false);
			
			// Process the triggered ability through the normal invoke flow
			await HandleAbilityInvokeAsync(triggerInvoke);
			
			logger.LogSuccess($"Successfully triggered ability: {ability.abilityName}", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to trigger ability by instance {instancedAbilityId}: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Trigger an ability by its hash directly
	/// </summary>
	/// <param name="abilityHash">The ability hash</param>
	/// <param name="ability">The ability configuration</param>
	protected virtual async Task TriggerAbilityByHash(uint abilityHash, ConfigAbility ability)
	{
		try
		{
			// Find or create an instance ID for this ability
			uint instancedAbilityId = 0;
			
			// Try to find existing instance ID for this ability hash
			var existingInstance = InstanceToAbilityHashMap.FirstOrDefault(kvp => kvp.Value == abilityHash);
			if (existingInstance.Key != 0)
			{
				instancedAbilityId = existingInstance.Key;
			}
			else
			{
				logger.LogWarning($"Instance ID {instancedAbilityId} for ability hash {abilityHash}");
				return; // Cannot trigger ability without a valid instance ID
			}
			
			// Create a new ability invoke entry for the triggered ability
			var triggerInvoke = new AbilityInvokeEntry
			{
				Head = new AbilityInvokeEntryHead
				{
					InstancedAbilityId = instancedAbilityId,
					LocalId = 0,
					TargetId = Owner._EntityId
				},
				EntityId = Owner._EntityId,
				ArgumentType = AbilityInvokeArgument.AbilityNone,
				AbilityData = Array.Empty<byte>()
			};
			
			logger.LogInfo($"Triggering ability by hash: {ability.abilityName} (Hash={abilityHash}, InstanceId={instancedAbilityId})", false);
			
			// Process the triggered ability through the normal invoke flow
			await HandleAbilityInvokeAsync(triggerInvoke);
			
			logger.LogSuccess($"Successfully triggered ability by hash: {ability.abilityName}", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to trigger ability by hash {abilityHash}: {ex.Message}");
		}
	}

	protected virtual void ReInitOverrideMap(uint abilityNameHash, AbilityMetaReInitOverrideMap? overrideMap)
	{
		if (overrideMap == null)
			return;
		foreach (var entry in overrideMap.OverrideMaps)
		{
			if (entry.ValueType != AbilityScalarType.AbilityScalarTypeFloat)
			{
				logger.LogWarning($"Unhandled value type {entry.ValueType} in Config {ConfigAbilityHashMap[abilityNameHash].abilityName}");
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

	protected virtual void AddAbility(AbilityAppliedAbility ability)
	{
		uint hash = ability.AbilityName.Hash;
		uint instancedId = ability.InstancedAbilityId;
		InstanceToAbilityHashMap[instancedId] = hash;
		if (ability.OverrideMaps.Any())
		{
			foreach (var entry in ability.OverrideMaps)
			{
				switch (entry.ValueType)
				{
					case AbilityScalarType.AbilityScalarTypeFloat:
						try
						{
							AbilitySpecialOverrideMap[hash][entry.Key.Hash] = entry.FloatValue;
						}
						catch
						{
							//TODO fix missing ability hashes
							AbilitySpecialOverrideMap[hash] = new();
							AbilitySpecialOverrideMap[hash][entry.Key.Hash] = entry.FloatValue;
						}
						break;
					default:
						logger.LogError($"Unhandled value type {entry.ValueType} in Config {ConfigAbilityHashMap[hash].abilityName}");
						break;
				}
			}
		}
	}
}