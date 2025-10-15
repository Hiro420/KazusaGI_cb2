
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using ProtoBuf;
using Newtonsoft.Json;

namespace KazusaGI_cb2.GameServer.Ability
{
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

		private static readonly Dictionary<Type, AbilityMixinHandler> mixinHandlers = new();

		static BaseAbilityManager()
		{
			RegisterMixinHandlers();
		}

		public Dictionary<uint, uint> InstanceToAbilityHashMap = new();
		public static Dictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } = GetDefaultConfigAbilityHashMap();
		public readonly Dictionary<uint, Dictionary<uint, float>> AbilitySpecialOverrideMap = new();
		public abstract Dictionary<string, Dictionary<string, float>?>? AbilitySpecials { get; }
		public abstract HashSet<string> ActiveDynamicAbilities { get; }
		public abstract Dictionary<string, HashSet<string>> UnlockedTalentParams { get; }
		protected Dictionary<uint, string> AbilitySpecialHashMap = new();

		public Dictionary<AbilityString, AbilityScalarValueEntry> GlobalValueHashMap = new();

		protected Dictionary<int, ActiveModifierInfo> ActiveModifiers = new();
		protected Dictionary<uint, HashSet<int>> EntityModifiers = new();

		protected BaseAbilityManager(Entity owner)
		{
			Owner = owner;
		}

		public bool IsInited { get; set; } = false;

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

		public virtual async Task ExecuteMixinAsync(ConfigAbility ability, BaseAbilityMixin mixin, Entity source, Entity? target = null)
		{
			var mixinType = mixin.GetType();
			if (!mixinHandlers.TryGetValue(mixinType, out var handler))
			{
				logger.LogWarning($"No handler registered for mixin type: {mixinType.Name}");
				return;
			}

			try
			{
				var result = await handler.ExecuteAsync(ability, mixin, source, target);
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
					ProcessModifierAction(modifierChange);
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
					if (asEntry != null)
					{
						AbilityString? existing = GlobalValueHashMap.Keys.FirstOrDefault(k => k.Hash == asEntry.Key.Hash);
						if (existing != null)
							GlobalValueHashMap.Remove(existing);
						GlobalValueHashMap[asEntry.Key] = asEntry;
					}
					break;
				case AbilityInvokeArgument.AbilityMetaAddNewAbility:
					info = Serializer.Deserialize<AbilityMetaAddAbility>(data);
					AddAbility((info as AbilityMetaAddAbility).Ability);
					break;
				case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
					// not implemented
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
						//Owner.ForceKill();
					}
					break;
				default:
					logger.LogWarning($"Unhandled AbilityInvokeArgument: {invoke.ArgumentType}");
					break;
			}
		}

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
				logger.LogError($"{ex.StackTrace}");
			}
		}

		protected virtual void ProcessAddModifier(AbilityMetaModifierChange modifierChange)
		{
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

			if (modifierChange.AttachedInstancedModifier != null && modifierChange.AttachedInstancedModifier.OwnerEntityId != 0)
			{
				var attached = modifierChange.AttachedInstancedModifier;
			}

			try
			{
				uint targetEntityId = modifierChange.ApplyEntityId != 0 ? modifierChange.ApplyEntityId : Owner._EntityId;

				var modifierInfo = new ActiveModifierInfo(modifierChange.ModifierLocalId, targetEntityId, modifierChange.AttachedInstancedModifier?.OwnerEntityId ?? Owner._EntityId)
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
					EntityModifiers[targetEntityId] = new HashSet<int>();
				EntityModifiers[targetEntityId].Add(modifierChange.ModifierLocalId);

				if (modifierChange.AttachedInstancedModifier != null && modifierChange.AttachedInstancedModifier.InstancedModifierId != 0)
				{
					uint instModId = modifierChange.AttachedInstancedModifier.InstancedModifierId;
					uint ownerId = modifierChange.AttachedInstancedModifier.OwnerEntityId != 0 ? modifierChange.AttachedInstancedModifier.OwnerEntityId : Owner._EntityId;
					try
					{
						var session = Owner.session;
						if (session != null && session.entityMap.TryGetValue(modifierInfo.ApplyEntityId, out var targetEntity))
						{
							var ctrl = new AbilityModifierController(null, null, null);
							ctrl.Initialize(instModId, ownerId, modifierInfo.ApplyEntityId);
							targetEntity.InstancedModifiers[instModId] = ctrl;
						}
					}
					catch { }
				}
			}
			catch (Exception ex)
			{
				logger.LogError($"Failed to apply modifier: {ex.Message}");
			}

			ConfigAbility? ability = null;
			if (InstanceToAbilityHashMap.TryGetValue((uint)modifierChange.ModifierLocalId, out uint abilityHash)
				&& ConfigAbilityHashMap.TryGetValue(abilityHash, out ability))
			{
				if (ability.LocalIdToInvocationMap.TryGetValue((uint)modifierChange.ModifierLocalId, out IInvocation? invocation))
				{
					if (invocation is BaseAbilityMixin mixin)
					{
						logger.LogSuccess($"Executing mixin: {mixin.GetType().Name} for ability: {ability.abilityName}", false);
						_ = ExecuteMixinAsync(ability, mixin, Owner);
						return;
					}
					else
					{
						logger.LogSuccess($"Invoking ability action: {ability.abilityName} ModifierLocalId={modifierChange.ModifierLocalId}", false);
						_ = invocation.Invoke(ability.abilityName, Owner);
						return;
					}
				}
				else
				{
					logger.LogWarning($"Invocation not found: ModifierLocalId={modifierChange.ModifierLocalId} in ability {ability.abilityName}");
					logger.LogWarning($"LocalIdToInvocationMap {JsonConvert.SerializeObject(ability.LocalIdToInvocationMap)}");
					return;
				}
			}
		}

		protected virtual void ProcessRemoveModifier(AbilityMetaModifierChange modifierChange)
		{
			if (modifierChange.AttachedInstancedModifier != null)
			{
				var attached = modifierChange.AttachedInstancedModifier;
			}

			try
			{
				if (ActiveModifiers.TryGetValue(modifierChange.ModifierLocalId, out var modifierInfo))
				{
					if (EntityModifiers.TryGetValue(modifierInfo.ApplyEntityId, out var entityMods))
					{
						entityMods.Remove(modifierChange.ModifierLocalId);
						if (entityMods.Count == 0)
							EntityModifiers.Remove(modifierInfo.ApplyEntityId);
					}

					ActiveModifiers.Remove(modifierChange.ModifierLocalId);
				}
				else
				{
					if (modifierChange.AttachedInstancedModifier != null)
					{
						var instancedId = modifierChange.AttachedInstancedModifier.InstancedModifierId;
						var foundModifier = ActiveModifiers.Values.FirstOrDefault(m => m.InstancedModifierId == instancedId);
						if (foundModifier != null)
							RemoveModifierByLocalId(foundModifier.LocalId);
					}
				}
			}
			catch (Exception ex)
			{
				logger.LogError($"Failed to remove modifier: {ex.Message}");
			}
		}

		protected virtual bool RemoveModifierByLocalId(int localId)
		{
			if (ActiveModifiers.TryGetValue(localId, out var modifierInfo))
			{
				if (EntityModifiers.TryGetValue(modifierInfo.ApplyEntityId, out var entityMods))
				{
					entityMods.Remove(localId);
					if (entityMods.Count == 0)
						EntityModifiers.Remove(modifierInfo.ApplyEntityId);
				}

				ActiveModifiers.Remove(localId);
				return true;
			}
			return false;
		}

		public virtual void RemoveAllModifiersFromEntity(uint entityId)
		{
			if (EntityModifiers.TryGetValue(entityId, out var modifierIds))
			{
				var idsToRemove = modifierIds.ToList();
				foreach (var modifierId in idsToRemove)
					RemoveModifierByLocalId(modifierId);
				logger.LogInfo($"Removed {idsToRemove.Count} modifiers from entity {entityId}", false);
			}
		}

		public virtual Dictionary<int, ActiveModifierInfo> GetActiveModifiers() => new(ActiveModifiers);
		public virtual int GetActiveModifierCount() => ActiveModifiers.Count;
		public virtual List<ActiveModifierInfo> GetModifiersForEntity(uint entityId)
			=> !EntityModifiers.TryGetValue(entityId, out var ids) ? new List<ActiveModifierInfo>() : ids.Where(id => ActiveModifiers.ContainsKey(id)).Select(id => ActiveModifiers[id]).ToList();

		// Public helper used by action invokes
		public virtual void AddActiveModifierForEntity(Entity applyEntity, ConfigAbility ability, AbilityModifierController ctrl, string modifierName)
		{
			// Create tracking info using next available local id (use modifier local id as a simple hash here)
			int localId = (int)ctrl.InstancedModifierId;
			var info = new ActiveModifierInfo(localId, applyEntity._EntityId, Owner._EntityId)
			{
				ParentAbilityName = ability.abilityName,
				Properties = new List<ModifierProperty>()
			};
			ActiveModifiers[localId] = info;
			if (!EntityModifiers.ContainsKey(applyEntity._EntityId))
				EntityModifiers[applyEntity._EntityId] = new HashSet<int>();
			EntityModifiers[applyEntity._EntityId].Add(localId);
		}

		public virtual bool RemoveActiveModifierByLocalId(int localId)
		{
			return RemoveModifierByLocalId(localId);
		}

		public virtual bool TryGetAbilityByName(string abilityName, out ConfigAbility? ability)
		{
			ability = null;
			uint hash = Utils.AbilityHash(abilityName);
			if (ConfigAbilityHashMap.TryGetValue(hash, out var cfg))
			{
				ability = cfg;
				return true;
			}
			return false;
		}

		protected virtual void ProcessTriggerAbilityAction(AbilityActionTriggerAbility? triggerAction)
		{
			if (triggerAction == null) { logger.LogWarning("ProcessTriggerAbilityAction called with null triggerAction"); return; }
			try
			{
				if (InstanceToAbilityHashMap.TryGetValue(triggerAction.OtherId, out uint abilityHash) && ConfigAbilityHashMap.TryGetValue(abilityHash, out var targetAbility))
				{
					_ = TriggerAbilityByInstance(triggerAction.OtherId, targetAbility);
				}
				else if (ConfigAbilityHashMap.TryGetValue(triggerAction.OtherId, out targetAbility))
				{
					_ = TriggerAbilityByHash(triggerAction.OtherId, targetAbility);
				}
				else logger.LogWarning($"Could not find ability to trigger: OtherId={triggerAction.OtherId}");
			}
			catch (Exception ex) { logger.LogError($"Error processing trigger ability action: {ex.Message}"); }
		}

		protected virtual async Task TriggerAbilityByInstance(uint instancedAbilityId, ConfigAbility ability)
		{
			try
			{
				var triggerInvoke = new AbilityInvokeEntry { Head = new AbilityInvokeEntryHead { InstancedAbilityId = instancedAbilityId, LocalId = 0, TargetId = Owner._EntityId }, EntityId = Owner._EntityId, ArgumentType = AbilityInvokeArgument.AbilityNone, AbilityData = Array.Empty<byte>() };
				logger.LogInfo($"Triggering ability by instance: {ability.abilityName} (InstanceId={instancedAbilityId})", false);
				await HandleAbilityInvokeAsync(triggerInvoke);
				logger.LogSuccess($"Successfully triggered ability: {ability.abilityName}", false);
			}
			catch (Exception ex) { logger.LogError($"Failed to trigger ability by instance {instancedAbilityId}: {ex.Message}"); }
		}

		protected virtual async Task TriggerAbilityByHash(uint abilityHash, ConfigAbility ability)
		{
			try
			{
				var existingInstance = InstanceToAbilityHashMap.FirstOrDefault(kvp => kvp.Value == abilityHash);
				if (existingInstance.Key == 0) { logger.LogWarning($"Instance not found for ability hash {abilityHash}"); return; }
				uint instancedAbilityId = existingInstance.Key;
				var triggerInvoke = new AbilityInvokeEntry { Head = new AbilityInvokeEntryHead { InstancedAbilityId = instancedAbilityId, LocalId = 0, TargetId = Owner._EntityId }, EntityId = Owner._EntityId, ArgumentType = AbilityInvokeArgument.AbilityNone, AbilityData = Array.Empty<byte>() };
				logger.LogInfo($"Triggering ability by hash: {ability.abilityName} (Hash={abilityHash}, InstanceId={instancedAbilityId})", false);
				await HandleAbilityInvokeAsync(triggerInvoke);
				logger.LogSuccess($"Successfully triggered ability by hash: {ability.abilityName}", false);
			}
			catch (Exception ex) { logger.LogError($"Failed to trigger ability by hash {abilityHash}: {ex.Message}"); }
		}

		protected virtual void ReInitOverrideMap(uint abilityNameHash, AbilityMetaReInitOverrideMap? overrideMap)
		{
			if (overrideMap == null) return;
			foreach (var entry in overrideMap.OverrideMaps)
			{
				if (entry.ValueType != AbilityScalarType.AbilityScalarTypeFloat) { logger.LogWarning($"Unhandled value type {entry.ValueType}"); continue; }
				try { AbilitySpecialOverrideMap[abilityNameHash][entry.Key.Hash] = entry.FloatValue; }
				catch { AbilitySpecialOverrideMap[abilityNameHash] = new(); AbilitySpecialOverrideMap[abilityNameHash][entry.Key.Hash] = entry.FloatValue; }
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
					if (entry.ValueType == AbilityScalarType.AbilityScalarTypeFloat)
					{
						try { AbilitySpecialOverrideMap[hash][entry.Key.Hash] = entry.FloatValue; }
						catch { AbilitySpecialOverrideMap[hash] = new(); AbilitySpecialOverrideMap[hash][entry.Key.Hash] = entry.FloatValue; }
					}
				}
			}
		}

		protected static Dictionary<uint, ConfigAbility> GetDefaultConfigAbilityHashMap()
		{
			var map = new Dictionary<uint, ConfigAbility>();
			foreach (var kvp in MainApp.resourceManager.ConfigAbilityMap)
			{
				if (kvp.Value.Default is ConfigAbility ability)
				{
					uint hash = Utils.AbilityHash(ability.abilityName);
					map[hash] = ability;
				}
			}
			return map;
		}

	}
	
}