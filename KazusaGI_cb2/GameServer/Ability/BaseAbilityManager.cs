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
/// Base player manager class that provides common functionality for all player-related managers.
/// Each entity type (Avatar, Gadget, Monster) should have its own ability manager inheriting from this.
/// </summary>
public abstract class BasePlayerManager
{
    protected readonly Player player;
    
    protected BasePlayerManager(Player player)
    {
        this.player = player;
    }
}

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

public abstract class BaseAbilityManager : BasePlayerManager
{
	public static readonly Logger logger = new("AbilityManager");
	protected readonly Entity Owner;
	
	/// <summary>
	/// Static registry of mixin handlers
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

	public Dictionary<AbilityString, AbilityScalarValueEntry> GlobalValueHashMap = new(); // <hash, value>
	
	/// <summary>
	/// Tracks active modifiers by their local ID for easy removal
	/// </summary>
	protected Dictionary<int, ActiveModifierInfo> ActiveModifiers = new(); // <modifierLocalId, modifierInfo>
	
	/// <summary>
	/// Tracks active modifiers by entity ID for entity cleanup
	/// </summary>
	protected Dictionary<uint, HashSet<int>> EntityModifiers = new(); // <entityId, set of modifierLocalIds>
	
	protected BaseAbilityManager(Entity owner) : base(owner.World.Host)
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
	/// Execute a mixin with the specified parameters
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
		if (AbilitySpecials == null) return;
		
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
				logger.LogInfo($"Found ability: {ability.abilityName} for LocalId={invoke.Head.LocalId}");
				logger.LogInfo($"LocalIdToInvocationMap has {ability.LocalIdToInvocationMap?.Count ?? 0} entries");
				
				// Found specific ability - check for mixins/actions
				if (ability.LocalIdToInvocationMap?.TryGetValue((uint)invoke.Head.LocalId, out IInvocation invocation) == true)
				{
					logger.LogInfo($"Found invocation for LocalId {invoke.Head.LocalId}: {invocation.GetType().Name}");
					if (invocation is BaseAbilityMixin mixin)
					{
						logger.LogInfo($"Executing mixin: {mixin.GetType().Name}");
						await ExecuteMixinAsync(ability, mixin, invoke.AbilityData, Owner);
						return; // Mixin processed, don't continue to argument type processing
					}
					else
                    {
                        logger.LogInfo($"Executing action: {invocation.GetType().Name}");
                        await invocation.Invoke(ability.abilityName, Owner);
						return; // Action processed, don't continue to argument type processing
					}
				}
				else
				{
					logger.LogInfo($"LocalId {invoke.Head.LocalId} not found in LocalIdToInvocationMap (this is normal for modifier changes)");
				}
			}
			else
			{
				logger.LogInfo($"No ability found for InstancedAbilityId={invoke.Head.InstancedAbilityId}");
			}
		}
		
		switch (invoke.ArgumentType)
		{
			case AbilityInvokeArgument.AbilityNone:
				logger.LogError($"Missing localId: {invoke.Head.LocalId}, ability: {invoke.Head.InstancedAbilityId}");
				info = new AbilityMetaModifierChange();
				break;
			case AbilityInvokeArgument.AbilityMetaModifierChange:
				info = Serializer.Deserialize<AbilityMetaModifierChange>(data);
				var modifierChange = info as AbilityMetaModifierChange;
				logger.LogInfo($"Processing modifier change: Action={modifierChange?.Action}", false);
				await ProcessModifierAction(modifierChange);
				break;
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
			case AbilityInvokeArgument.AbilityMetaAddNewAbility:
				info = Serializer.Deserialize<AbilityMetaAddAbility>(data);
				AddAbility((info as AbilityMetaAddAbility).Ability);
				break;
			case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
				info = Serializer.Deserialize<AbilityMetaModifierDurabilityChange>(data);
				var durabilityChange = info as AbilityMetaModifierDurabilityChange;
				break;
			case AbilityInvokeArgument.AbilityActionTriggerAbility:
				info = Serializer.Deserialize<AbilityActionTriggerAbility>(data);
				var triggerAction = info as AbilityActionTriggerAbility;
				ProcessTriggerAbilityAction(triggerAction);
				break;
			case AbilityInvokeArgument.AbilityActionGenerateElemBall:
				info = Serializer.Deserialize<AbilityActionGenerateElemBall>(data);
				Owner.GenerateElemBall((AbilityActionGenerateElemBall)info);
				break;
			case AbilityInvokeArgument.AbilityMetaSetKilledSetate:
				info = Serializer.Deserialize<AbilityMetaSetKilledState>(data);
				AbilityMetaSetKilledState state = (AbilityMetaSetKilledState)info;
				if (state.Killed)
				{
					// Handle kill state
				}
				break;
			default:
				logger.LogWarning($"Unhandled AbilityInvokeArgument: {invoke.ArgumentType}");
				info = new AbilityMetaModifierChange();
				break;
		}
	}
	
	/// <summary>
	/// Process modifier action (add/remove modifiers)
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	protected virtual async Task ProcessModifierAction(AbilityMetaModifierChange? modifierChange)
	{
		if (modifierChange == null) return;
		
		try
		{
			switch (modifierChange.Action)
			{
				case ModifierAction.Added:
					await ProcessAddModifier(modifierChange);
					break;
				case ModifierAction.Removed:
					await ProcessRemoveModifier(modifierChange);
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
	protected virtual async Task ProcessAddModifier(AbilityMetaModifierChange modifierChange)
	{
		// Process modifier properties
		if (modifierChange.Properties.Count > 0)
		{
			foreach (var property in modifierChange.Properties)
			{
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
		
		// Track the modifier for proper lifecycle management
		try
		{
			uint targetEntityId = modifierChange.ApplyEntityId != 0 ? modifierChange.ApplyEntityId : Owner._EntityId;
			
			var modifierInfo = new ActiveModifierInfo(
				modifierChange.ModifierLocalId, 
				targetEntityId, 
				modifierChange.AttachedInstancedModifier?.OwnerEntityId ?? Owner._EntityId)
			{
				ParentAbilityName = modifierChange.ParentAbilityName?.Str,
				Properties = modifierChange.Properties.ToList()
			};
			
			if (modifierChange.AttachedInstancedModifier != null)
			{
				modifierInfo.InstancedModifierId = modifierChange.AttachedInstancedModifier.InstancedModifierId;
			}
			
			ActiveModifiers[modifierChange.ModifierLocalId] = modifierInfo;
			
			if (!EntityModifiers.ContainsKey(targetEntityId))
			{
				EntityModifiers[targetEntityId] = new HashSet<int>();
			}
			EntityModifiers[targetEntityId].Add(modifierChange.ModifierLocalId);
			
			// Execute modifier's onAdded actions
			await ExecuteModifierActions(modifierChange, true);
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
	protected virtual async Task ProcessRemoveModifier(AbilityMetaModifierChange modifierChange)
	{
		try
		{
			// Execute modifier's onRemoved actions before removing
			await ExecuteModifierActions(modifierChange, false);
			
			if (ActiveModifiers.TryGetValue(modifierChange.ModifierLocalId, out var modifierInfo))
			{
				if (EntityModifiers.TryGetValue(modifierInfo.ApplyEntityId, out var entityMods))
				{
					entityMods.Remove(modifierChange.ModifierLocalId);
					if (entityMods.Count == 0)
					{
						EntityModifiers.Remove(modifierInfo.ApplyEntityId);
					}
				}
				
				ActiveModifiers.Remove(modifierChange.ModifierLocalId);
			}
			else if (modifierChange.AttachedInstancedModifier != null)
			{
				var instancedId = modifierChange.AttachedInstancedModifier.InstancedModifierId;
				var foundModifier = ActiveModifiers.Values.FirstOrDefault(m => m.InstancedModifierId == instancedId);
				
				if (foundModifier != null)
				{
					RemoveModifierByLocalId(foundModifier.LocalId);
				}
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to remove modifier: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Remove a modifier by its LocalId
	/// </summary>
	protected virtual bool RemoveModifierByLocalId(int localId)
	{
		if (ActiveModifiers.TryGetValue(localId, out var modifierInfo))
		{
			if (EntityModifiers.TryGetValue(modifierInfo.ApplyEntityId, out var entityMods))
			{
				entityMods.Remove(localId);
				if (entityMods.Count == 0)
				{
					EntityModifiers.Remove(modifierInfo.ApplyEntityId);
				}
			}
			
			ActiveModifiers.Remove(localId);
			return true;
		}
		return false;
	}
	
	/// <summary>
	/// Remove all modifiers from a specific entity
	/// </summary>
	public virtual void RemoveAllModifiersFromEntity(uint entityId)
	{
		if (EntityModifiers.TryGetValue(entityId, out var modifierIds))
		{
			var idsToRemove = modifierIds.ToList();
			foreach (var modifierId in idsToRemove)
			{
				RemoveModifierByLocalId(modifierId);
			}
		}
	}
	
	/// <summary>
	/// Process trigger ability action
	/// </summary>
	protected virtual void ProcessTriggerAbilityAction(AbilityActionTriggerAbility? triggerAction)
	{
		if (triggerAction == null) return;

		logger.LogWarning($"ProcessTriggerAbilityAction {triggerAction.OtherId}");
		
		try
		{
			ConfigAbility? targetAbility = null;
			if (InstanceToAbilityHashMap.TryGetValue(triggerAction.OtherId, out uint abilityHash)
				&& ConfigAbilityHashMap.TryGetValue(abilityHash, out targetAbility))
			{
				_ = TriggerAbilityByInstance(triggerAction.OtherId, targetAbility);
			}
			else if (ConfigAbilityHashMap.TryGetValue(triggerAction.OtherId, out targetAbility))
			{
				_ = TriggerAbilityByHash(triggerAction.OtherId, targetAbility);
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
	protected virtual async Task TriggerAbilityByInstance(uint instancedAbilityId, ConfigAbility ability)
    {
        try
		{
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
			
			await HandleAbilityInvokeAsync(triggerInvoke);
		}
		catch (Exception ex)
		{
			logger.LogError($"Failed to trigger ability by instance {instancedAbilityId}: {ex.Message}");
		}
	}
	
	/// <summary>
	/// Trigger an ability by its hash directly
	/// </summary>
	protected virtual async Task TriggerAbilityByHash(uint abilityHash, ConfigAbility ability)
	{
		try
		{
			var existingInstance = InstanceToAbilityHashMap.FirstOrDefault(kvp => kvp.Value == abilityHash);
			if (existingInstance.Key == 0) return;
			
			var triggerInvoke = new AbilityInvokeEntry
			{
				Head = new AbilityInvokeEntryHead
				{
					InstancedAbilityId = existingInstance.Key,
					LocalId = 0,
					TargetId = Owner._EntityId
				},
				EntityId = Owner._EntityId,
				ArgumentType = AbilityInvokeArgument.AbilityNone,
				AbilityData = Array.Empty<byte>()
			};
			
			await HandleAbilityInvokeAsync(triggerInvoke);
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
	
	/// <summary>
	/// Execute actions associated with a modifier being added or removed
	/// </summary>
	/// <param name="modifierChange">The modifier change data</param>
	/// <param name="isAdding">True if adding modifier (execute onAdded), false if removing (execute onRemoved)</param>
	protected virtual async Task ExecuteModifierActions(AbilityMetaModifierChange modifierChange, bool isAdding)
	{
		try
		{
			// Find the parent ability
			string? parentAbilityName = modifierChange.ParentAbilityName?.Str;
			if (string.IsNullOrEmpty(parentAbilityName))
			{
				logger.LogInfo("No parent ability name for modifier action execution");
				return;
			}
			
			// Find the ability configuration
			var abilityHash = ConfigAbilityHashMap.FirstOrDefault(kvp => kvp.Value.abilityName == parentAbilityName);
			if (abilityHash.Value == null)
			{
				logger.LogInfo($"Parent ability {parentAbilityName} not found in ConfigAbilityHashMap");
				return;
			}
			
			ConfigAbility ability = abilityHash.Value;
			
			// Find the specific modifier in the ability
			if (ability.modifiers == null)
			{
				logger.LogInfo($"No modifiers found in ability {parentAbilityName}");
				return;
			}
			
			// Try to find modifier by name (this might need adjustment based on how modifier names are stored)
			AbilityModifier? modifier = null;
			foreach (var mod in ability.modifiers.Values)
			{
				// For now, we'll execute the first modifier's actions
				// TODO: Find the correct modifier based on modifierChange data
				modifier = mod;
				break;
			}
			
			if (modifier == null)
			{
				logger.LogInfo($"No matching modifier found in ability {parentAbilityName}");
				return;
			}
			
			// Execute the appropriate actions
			BaseAction[]? actionsToExecute = isAdding ? modifier.onAdded : modifier.onRemoved;
			if (actionsToExecute == null || actionsToExecute.Length == 0)
			{
				logger.LogInfo($"No {(isAdding ? "onAdded" : "onRemoved")} actions for modifier in {parentAbilityName}");
				return;
			}
			
			logger.LogInfo($"Executing {actionsToExecute.Length} {(isAdding ? "onAdded" : "onRemoved")} actions for modifier in {parentAbilityName}");
			
			// Execute each action
			foreach (var action in actionsToExecute)
			{
				if (action == null) continue;
				
				logger.LogInfo($"Executing modifier action: {action.GetType().Name}");
				
				// Use the action's Invoke method directly
				await action.Invoke(parentAbilityName, Owner, null);
			}
		}
		catch (Exception ex)
		{
			logger.LogError($"Error executing modifier actions: {ex.Message}");
		}
	}
}