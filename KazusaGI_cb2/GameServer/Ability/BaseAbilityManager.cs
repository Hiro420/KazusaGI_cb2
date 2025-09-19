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
	protected Dictionary<uint, uint> InstanceToAbilityHashMap = new(); // <instancedAbilityId, abilityNameHash>
	protected abstract Dictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } // <abilityNameHash, configAbility>
	public readonly Dictionary<uint, Dictionary<uint, float>> AbilitySpecialOverrideMap = new(); // <abilityNameHash, <abilitySpecialNameHash, value>>
	public abstract Dictionary<string, Dictionary<string, float>?>? AbilitySpecials { get; }// <abilityName, <abilitySpecial, value>>
	public abstract HashSet<string> ActiveDynamicAbilities { get; }
	public abstract Dictionary<string, HashSet<string>> UnlockedTalentParams { get; }
	protected Dictionary<uint, string> AbilitySpecialHashMap = new(); // <hash, abilitySpecialName>

	protected Dictionary<uint, float> GlobalValueHashMap = new(); // <hash, value> TODO map the hashes to variable names
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
				break;
			case AbilityInvokeArgument.AbilityMetaSpecialFloatArgument:
				info = Serializer.Deserialize<AbilityMetaSpecialFloatArgument>(data);
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
				GlobalValueHashMap[asEntry.Key.Hash] = asEntry.FloatValue;
				break;
			case AbilityInvokeArgument.AbilityMetaAddOrGetAbilityAndTrigger:
				info = Serializer.Deserialize<AbilityMetaAddOrGetAbilityAndTrigger>(data);
				break;
			case AbilityInvokeArgument.AbilityMetaAddNewAbility:
				info = Serializer.Deserialize<AbilityMetaAddAbility>(data);
				AddAbility((info as AbilityMetaAddAbility).Ability);
				break;
			case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
				info = Serializer.Deserialize<AbilityMetaModifierDurabilityChange>(data);
				var durabilityChange = info as AbilityMetaModifierDurabilityChange;
				logger.LogInfo($"Processing modifier durability change: {invoke.Head.InstancedModifierId}", false);
				break;
			case AbilityInvokeArgument.AbilityActionTriggerAbility:
				info = Serializer.Deserialize<AbilityActionTriggerAbility>(data);
				break;
			case AbilityInvokeArgument.AbilityActionGenerateElemBall:
				info = Serializer.Deserialize<AbilityActionGenerateElemBall>(data);
				Owner.GenerateElemBall((AbilityActionGenerateElemBall)info);
				break;
			case AbilityInvokeArgument.AbilityMixinWindZone:
				info = Serializer.Deserialize<AbilityMixinWindZone>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinCostStamina:
				info = Serializer.Deserialize<AbilityMixinCostStamina>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinGlobalShield:
				info = Serializer.Deserialize<AbilityMixinGlobalShield>(data);
				break;
			case AbilityInvokeArgument.AbilityMixinWindSeedSpawner:
				info = Serializer.Deserialize<AbilityMixinWindSeedSpawner>(data);
				break;
			default:
				logger.LogWarning($"Unhandled AbilityInvokeArgument: {invoke.ArgumentType}");
				info = new AbilityMetaModifierChange(); // should not happen, just to satisfy the compiler
				break;
		}

		logger.LogInfo($"RECV ability invoke: {invoke} {info.GetType()} {Owner._EntityId}", true);
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