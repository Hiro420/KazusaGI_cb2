using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public abstract class BaseAbilityManager
{
	public static readonly Logger logger = new("AbilityManager");
	protected readonly Entity Owner;
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
		//TODO add all cases
		switch (invoke.ArgumentType)
		{
			case AbilityInvokeArgument.AbilityNone:
				//TODO
				ConfigAbility ability = ConfigAbilityHashMap[InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]];
				if (ability.LocalIdToInvocationMap.TryGetValue((uint)invoke.Head.LocalId, out IInvocation invocation))
					await invocation.Invoke(ability.abilityName, Owner);
				else
					logger.LogError($"Missing localId: {invoke.Head.LocalId}, ability: {invoke.Head.InstancedAbilityId}");
				info = new AbilityMetaModifierChange(); // just to satisfy the compiler. In this case abilityData is empty anyway.
				break;
			case AbilityInvokeArgument.AbilityMetaModifierChange:
				info = Serializer.Deserialize<AbilityMetaModifierChange>(data);
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
				break;
			case AbilityInvokeArgument.AbilityActionTriggerAbility:
				info = Serializer.Deserialize<AbilityActionTriggerAbility>(data);
				break;
			case AbilityInvokeArgument.AbilityActionGenerateElemBall:
				info = Serializer.Deserialize<AbilityActionGenerateElemBall>(data);
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