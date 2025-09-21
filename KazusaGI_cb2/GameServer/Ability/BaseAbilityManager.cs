using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace KazusaGI_cb2.GameServer.Ability;

/// <summary>
/// Information about an active modifier for tracking purposes
/// </summary>
public class ActiveModifierInfo
{
	public int LocalId { get; set; }
	public uint? InstancedModifierId { get; set; }
	public uint ApplyEntityId { get; set; }
	public uint OwnerEntityId { get; set; }
	public string? ParentAbilityName { get; set; }
	public DateTime AppliedTime { get; set; }
	public List<ModifierProperty> Properties { get; set; } = new();
	
	public ActiveModifierInfo(int localId, uint applyEntityId, uint ownerEntityId)
	{
		LocalId = localId;
		ApplyEntityId = applyEntityId;
		OwnerEntityId = ownerEntityId;
		AppliedTime = DateTime.UtcNow;
	}
}

public abstract class BaseAbilityManager
{
	public static readonly Logger logger = new("AbilityManager");
	protected readonly Entity Owner;
	
	/// <summary>
	/// Static registry of mixin handlers, similar to GC's mixinHandlers HashMap
	/// </summary>
	private static readonly Dictionary<Type, AbilityMixinHandler> mixinHandlers = new();
	
	/// <summary>
	/// Static constructor to register mixin handlers automatically
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
		
		// First, try to handle server-sided invokes (when LocalId != 0) for specific abilities
		if (invoke.Head.LocalId != 0)
		{
			logger.LogInfo($"Server-sided ability invoke: LocalId={invoke.Head.LocalId}, " +
				$"ArgumentType={invoke.ArgumentType}, EntityId={invoke.EntityId}, TargetId={invoke.Head.TargetId}", false);
			
			// Try to find the ability and execute mixin/action if found
			ConfigAbility? ability = null;
			if (invoke.Head.InstancedAbilityId != 0
				&& InstanceToAbilityHashMap.TryGetValue(invoke.Head.InstancedAbilityId, out uint abilityHash)
				&& ConfigAbilityHashMap.TryGetValue(abilityHash, out ability))
			{
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
			}
			
			// If no specific ability action/mixin was found, continue to process by argument type
			//logger.LogInfo($"No specific ability action found, processing by argument type: {invoke.ArgumentType}", false);
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
				ProcessModifierAction(invoke, modifierChange);
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
				info = Serializer.Deserialize<AbilityScalarValueEntry>(data);
				var asEntry = info as AbilityScalarValueEntry;
				GlobalValueHashMap[asEntry.Key] = asEntry;
				break;
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
				logger.LogInfo($"Processing modifier durability change: ModifierId={invoke.Head.InstancedModifierId}, " +
					$"ReduceDurability={durabilityChange?.ReduceDurability}, RemainDurability={durabilityChange?.RemainDurability}", false);
				ProcessModifierDurabilityChange(invoke, durabilityChange);
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
	/// <param name="entry">The ability invoke entry</param>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual void ProcessModifierAction(AbilityInvokeEntry entry, AbilityMetaModifierChange modifierChange)
	{
        if (modifierChange == null)
		{
			logger.LogWarning("ProcessModifierAction called with null modifierChange");
			return;
		}

		var head = entry.Head;

		if (head.InstancedAbilityId == 0 || head.InstancedModifierId > 2000)
			return; // Error: TODO: display error

		var entity = Owner.session.entityMap.GetValueOrDefault(entry.EntityId);
		if (entity == null)
		{
			logger.LogInfo($"Entity not found: {entry.EntityId}", false);
			return;
		}

        try
		{
			switch (modifierChange.Action)
			{
				case ModifierAction.Added:
					ProcessAddModifier(entry, modifierChange, entity);
					break;
					
				case ModifierAction.Removed:
					ProcessRemoveModifier(entry, modifierChange, entity);
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
	/// Process adding a modifier following Grasscutter's logic
	/// </summary>
	protected virtual void ProcessAddModifier(AbilityInvokeEntry entry, AbilityMetaModifierChange modifierChange, Entity entity)
	{
		var head = entry.Head;
		AbilityData? instancedAbilityData = null;
		InstancedAbility? instancedAbility = null;

		// First try to get ability from target entity if target is specified
		if (head.TargetId != 0)
		{
			var targetEntity = Owner.session.entityMap.GetValueOrDefault(head.TargetId);
			if (targetEntity != null)
			{
				if ((head.InstancedAbilityId - 1) < targetEntity.InstancedAbilities.Count)
				{
					instancedAbility = targetEntity.InstancedAbilities[(int)(head.InstancedAbilityId - 1)];
					if (instancedAbility != null) 
						instancedAbilityData = instancedAbility.Data;
				}
			}
		}

		// If not found, search on entity base id
		if (instancedAbilityData == null)
		{
			if (head.InstancedAbilityId > 0 && (head.InstancedAbilityId - 1) < entity.InstancedAbilities.Count)
			{
				instancedAbility = entity.InstancedAbilities[(int)(head.InstancedAbilityId - 1)];
				if (instancedAbility != null) 
					instancedAbilityData = instancedAbility.Data;
				logger.LogInfo($"Found ability at index {head.InstancedAbilityId - 1}: '{instancedAbility.Data?.AbilityName}'", false);
			}
			else
			{
				logger.LogWarning($"InstancedAbilityId {head.InstancedAbilityId} out of bounds. Entity has {entity.InstancedAbilities.Count} abilities (valid range: 1-{entity.InstancedAbilities.Count})");
			}
		}

		// If still not found, search for the parent ability by name
		if (instancedAbilityData == null)
		{
			var parentAbilityName = modifierChange.ParentAbilityName?.Str;
			logger.LogInfo($"Searching for parent ability: '{parentAbilityName}'", false);
			
			if (!string.IsNullOrEmpty(parentAbilityName))
			{
				logger.LogInfo($"ConfigAbilityMap has {MainApp.resourceManager.ConfigAbilityMap.Count} abilities loaded", false);
				
				var configAbility = MainApp.resourceManager.ConfigAbilityMap.GetValueOrDefault(parentAbilityName);
				if (configAbility != null)
				{
                    ConfigAbility configAbilityAsAbility = (ConfigAbility)configAbility.Default;
                    instancedAbilityData = new AbilityData(configAbilityAsAbility.abilityName, configAbilityAsAbility.modifiers);
					logger.LogInfo($"Found ability by parent name: '{configAbilityAsAbility.abilityName}'", false);
				}
				else
				{
					logger.LogInfo($"Parent ability '{parentAbilityName}' not found in ConfigAbilityMap", false);
					
					// Try to find a similar ability name (debug helper)
					var similarAbilities = MainApp.resourceManager.ConfigAbilityMap.Keys
						.Where(name => name.Contains(parentAbilityName) || parentAbilityName.Contains(name))
						.Take(5)
						.ToList();
					
					if (similarAbilities.Any())
					{
						logger.LogInfo($"Similar abilities found: {string.Join(", ", similarAbilities)}", false);
					}
				}
			}
		}

		if (instancedAbilityData == null)
		{
			logger.LogInfo($"No ability found for entity {entry.EntityId}, instancedAbilityId {head.InstancedAbilityId}, targetId {head.TargetId}", false);
			logger.LogInfo($"Entity has {entity.InstancedAbilities.Count} instanced abilities", false);
			
			// Debug: Show the abilities that are available
			if (entity.InstancedAbilities.Count > 0)
			{
				var abilityNames = entity.InstancedAbilities
					.Take(5)
					.Select((ability, index) => $"[{index + 1}] {ability.Data?.AbilityName ?? "null"}")
					.ToList();
				logger.LogInfo($"Available abilities: {string.Join(", ", abilityNames)}{(entity.InstancedAbilities.Count > 5 ? "..." : "")}", false);
			}
			return;
		}

		// Get modifier data by local id
		var modifiers = instancedAbilityData.Modifiers?.Values.ToArray();
		if (modifiers == null || modifierChange.ModifierLocalId >= modifiers.Length)
		{
			logger.LogInfo($"Modifier local id {modifierChange.ModifierLocalId} not found", false);
			return;
		}

		var modifierData = modifiers[modifierChange.ModifierLocalId];

		// Log the modifier addition
		if (entity.InstancedModifiers.ContainsKey(head.InstancedModifierId))
		{
			logger.LogInfo($"Replacing entity {entry.EntityId} modifier id {head.InstancedModifierId} with ability {instancedAbilityData.AbilityName} modifier {modifierData}", false);
		}
		else
		{
			logger.LogInfo($"Adding entity {entry.EntityId} modifier id {head.InstancedModifierId} with ability {instancedAbilityData.AbilityName} modifier {modifierData}", false);
		}

		// Handle possible elemental burst (equivalent to onPossibleElementalBurst in Grasscutter)
		// TODO: Implement elemental burst handling if needed

		// Create modifier controller
		var modifier = new AbilityModifierController(instancedAbility, instancedAbilityData, modifierData);

		// Add to entity's instanced modifiers
		entity.InstancedModifiers[head.InstancedModifierId] = modifier;

		// TODO: Add all the ability modifier property change
	}

	/// <summary>
	/// Process removing a modifier following Grasscutter's logic
	/// </summary>
	protected virtual void ProcessRemoveModifier(AbilityInvokeEntry entry, AbilityMetaModifierChange modifierChange, Entity entity)
	{
		var head = entry.Head;
		
		logger.LogInfo($"Removed on entity {entry.EntityId} modifier id {head.InstancedModifierId}: {entity.InstancedModifiers.GetValueOrDefault(head.InstancedModifierId)}", false);

		// Remove from entity's instanced modifiers
		entity.InstancedModifiers.Remove(head.InstancedModifierId);
	}

	/// <summary>
	/// Process modifier durability change, tracking modifier durability/lifetime
	/// </summary>
	/// <param name="entry">The ability invoke entry</param>
	/// <param name="durabilityChange">The durability change data</param>
	protected virtual void ProcessModifierDurabilityChange(AbilityInvokeEntry entry, AbilityMetaModifierDurabilityChange? durabilityChange)
	{
		if (durabilityChange == null)
		{
			logger.LogWarning("ProcessModifierDurabilityChange called with null durabilityChange");
			return;
		}

		var head = entry.Head;
		var entity = Owner.session.entityMap.GetValueOrDefault(entry.EntityId);
		
		if (entity == null)
		{
			logger.LogInfo($"Entity not found for durability change: {entry.EntityId}", false);
			return;
		}

		try
		{
			// Find the modifier controller
			if (entity.InstancedModifiers.TryGetValue(head.InstancedModifierId, out var modifierController))
			{
				logger.LogInfo($"Updating durability for modifier {head.InstancedModifierId} on entity {entry.EntityId}: " +
					$"Reduced by {durabilityChange.ReduceDurability}, remaining {durabilityChange.RemainDurability}", false);

				// Here you could track durability in the modifier controller if needed
				// For now, this is mainly informational logging
				
				// If durability reaches zero or below, the modifier might be automatically removed
				if (durabilityChange.RemainDurability <= 0)
				{
					logger.LogInfo($"Modifier {head.InstancedModifierId} durability depleted, may be removed soon", false);
				}
			}
			else
			{
				logger.LogInfo($"Modifier {head.InstancedModifierId} not found on entity {entry.EntityId} for durability change", false);
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Error processing modifier durability change: {ex.Message}");
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

	/// <summary>
	/// Add an ability to an entity by ability name, following Grasscutter's addAbilityToEntity pattern
	/// </summary>
	/// <param name="entity">The entity to add the ability to</param>
	/// <param name="abilityName">The name of the ability to add</param>
	public virtual void AddAbilityToEntity(Entity entity, string abilityName)
	{
		// Look up the ability data by name
		if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out var abilityContainer))
		{
			var configAbility = (ConfigAbility)abilityContainer.Default;
			AddAbilityToEntity(entity, configAbility);
		}
		else
		{
			logger.LogWarning($"Ability not found in ConfigAbilityMap: {abilityName}");
		}
	}

	/// <summary>
	/// Add an ability to an entity using ConfigAbility data, following Grasscutter's addAbilityToEntity pattern
	/// </summary>
	/// <param name="entity">The entity to add the ability to</param>
	/// <param name="configAbility">The ability configuration to add</param>
	public virtual void AddAbilityToEntity(Entity entity, ConfigAbility configAbility)
	{
		try
		{
			// Create ability data from the config
			var abilityData = new AbilityData(configAbility.abilityName, configAbility.modifiers);
			
			// Create an instanced ability
			var instancedAbility = new InstancedAbility(abilityData);
			
			// Add to the entity's instanced abilities list
			entity.InstancedAbilities.Add(instancedAbility);
			
			// Generate an instance ID for this ability (similar to Grasscutter's approach)
			uint instanceId = (uint)entity.InstancedAbilities.Count; // 1-based indexing to match Grasscutter
			
			// Add to the instance-to-hash mapping
			uint abilityHash = Utils.AbilityHash(configAbility.abilityName);
			InstanceToAbilityHashMap[instanceId] = abilityHash;
			
			// Add to the config ability hash map if not already present
			if (!ConfigAbilityHashMap.ContainsKey(abilityHash))
			{
				ConfigAbilityHashMap[abilityHash] = configAbility;
			}
			
			logger.LogInfo($"Added ability '{configAbility.abilityName}' to entity {entity._EntityId} at instance ID {instanceId}", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to add ability '{configAbility.abilityName}' to entity {entity._EntityId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Initialize default abilities for an entity based on its type
	/// This should be called when an entity is created or spawned
	/// </summary>
	/// <param name="entity">The entity to initialize abilities for</param>
	public virtual void InitializeEntityAbilities(Entity entity)
	{
		try
		{
			// Add some common default abilities that most entities should have
			// These would typically be determined by the entity type, avatar data, etc.
			
			// For now, add basic abilities that are commonly needed
			var commonAbilities = new[]
			{
				"Default_Avatar_CommonAbility", // Example common ability
				"Default_Entity_BaseAbility"    // Example base ability
			};

			foreach (var abilityName in commonAbilities)
			{
				// Only add if the ability exists in the resource manager
				if (MainApp.resourceManager.ConfigAbilityMap.ContainsKey(abilityName))
				{
					AddAbilityToEntity(entity, abilityName);
				}
			}
			
			logger.LogInfo($"Initialized abilities for entity {entity._EntityId}: {entity.InstancedAbilities.Count} abilities added", false);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to initialize abilities for entity {entity._EntityId}: {ex.Message}");
		}
	}

	/// <summary>
	/// Get an ability from an entity by instance ID (1-based indexing to match Grasscutter)
	/// </summary>
	/// <param name="entity">The entity to get the ability from</param>
	/// <param name="instancedAbilityId">The instance ID of the ability (1-based)</param>
	/// <returns>The instanced ability if found, null otherwise</returns>
	public virtual InstancedAbility? GetEntityAbility(Entity entity, uint instancedAbilityId)
	{
		if (instancedAbilityId == 0 || instancedAbilityId > entity.InstancedAbilities.Count)
		{
			return null;
		}
		
		return entity.InstancedAbilities[(int)(instancedAbilityId - 1)]; // Convert to 0-based index
	}

	/// <summary>
	/// Get ability data for a given ability name using the resource manager
	/// </summary>
	/// <param name="abilityName">The name of the ability</param>
	/// <returns>The ability data if found, null otherwise</returns>
	public virtual AbilityData? GetAbilityData(string abilityName)
	{
		if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out var abilityContainer))
		{
			var configAbility = (ConfigAbility)abilityContainer.Default;
			return new AbilityData(configAbility.abilityName, configAbility.modifiers);
		}
		
		return null;
	}

	protected virtual void AddAbility(AbilityAppliedAbility ability)
	{
		uint hash = ability.AbilityName.Hash;
		uint instancedId = ability.InstancedAbilityId;
		string abilityName = ability.AbilityName.Str ?? $"UnknownAbility_{hash}";
		
		// Update the instance mapping
		InstanceToAbilityHashMap[instancedId] = hash;
		
		// Try to get the actual ability configuration
		ConfigAbility? configAbility = null;
		if (MainApp.resourceManager.ConfigAbilityMap.TryGetValue(abilityName, out var abilityContainer))
		{
			configAbility = (ConfigAbility)abilityContainer.Default;
		}
		else if (ConfigAbilityHashMap.TryGetValue(hash, out configAbility))
		{
			// Found in the local config hash map
		}
		
		// Add the ability to the entity's InstancedAbilities if we have the config
		if (configAbility != null)
		{
			// Ensure the InstancedAbilities list is large enough for this instance ID
			while (Owner.InstancedAbilities.Count < instancedId)
			{
				// Add placeholder abilities for missing indices
				Owner.InstancedAbilities.Add(new InstancedAbility(null));
			}
			
			// Create ability data and instanced ability
			var abilityData = new AbilityData(configAbility.abilityName, configAbility.modifiers);
			var instancedAbility = new InstancedAbility(abilityData);
			
			// Set or add at the correct index (instancedId is 1-based)
			if (instancedId <= Owner.InstancedAbilities.Count)
			{
				Owner.InstancedAbilities[(int)(instancedId - 1)] = instancedAbility;
			}
			else
			{
				Owner.InstancedAbilities.Add(instancedAbility);
			}
			
			logger.LogInfo($"Added dynamic ability '{abilityName}' with instancedId {instancedId} (total abilities: {Owner.InstancedAbilities.Count})", false);
		}
		else
		{
			logger.LogWarning($"Could not find config for dynamic ability '{abilityName}' with hash {hash}");
		}
		
		// Handle override maps
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
						logger.LogError($"Unhandled value type {entry.ValueType} in Config {configAbility?.abilityName ?? "Unknown"}");
						break;
				}
			}
		}
	}
}