using Google.Protobuf;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using ProtoBuf;

namespace KazusaGI_cb2.GameServer.Systems.Ability;

public abstract class BaseAbilityManager
{
	protected static Logger logger = new("AbilityManager");
	protected readonly Entity Owner;
	// instancedAbilityId -> abilityNameHash
	protected Dictionary<uint, uint> InstanceToAbilityHashMap =>
		ConfigAbilityHashMap
			.Select((ability, index) => new
			{
				InstancedId = (uint)(index + 1), // or index if 0-based
				Hash = GameServer.Ability.Utils.AbilityHash(ability.Value.abilityName)
			})
			.ToDictionary(x => x.InstancedId, x => x.Hash);
	public abstract Dictionary<uint, ConfigAbility> ConfigAbilityHashMap { get; } // <abilityNameHash, configAbility>
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
	}
	public virtual async Task HandleAbilityInvokeAsync(AbilityInvokeEntry invoke)
	{
		MemoryStream data = new MemoryStream(invoke.AbilityData);

		if (invoke.Head.LocalId != 255) // INVOCATION_META_LOCALID = 255
		{

			logger.LogInfo($"Server-sided ability invoke: LocalId={invoke.Head.LocalId}, " +
				$"ArgumentType={invoke.ArgumentType}, EntityId={invoke.EntityId}, TargetId={invoke.Head.TargetId}");

			if (!InstanceToAbilityHashMap.ContainsKey(invoke.Head.InstancedAbilityId))
			{
				logger.LogWarning($"Missing instanced ability id: {invoke.Head.InstancedAbilityId}");
				foreach (var abilityEntry in InstanceToAbilityHashMap)
				{
					logger.LogInfo($"Known instanced ability id: {abilityEntry.Key} -> {abilityEntry.Value}");
				}
				return;
			}

			if (!ConfigAbilityHashMap.ContainsKey(InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]))
			{
				logger.LogWarning($"Missing ability config for ability id: {InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]}");
				return;
			}

			ConfigAbility ability = ConfigAbilityHashMap[InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]];

			if (ability.LocalIdToInvocationMap.TryGetValue((uint)invoke.Head.LocalId, out IInvocation? invocation))
			{
				logger.LogSuccess($"Invoking ability: {ability.abilityName}, localId: {invoke.Head.LocalId} | {invocation.GetType().Name}");
				await invocation.Invoke(ability.abilityName, Owner);
			}
			else
			{
				logger.LogWarning($"Missing localId: {invoke.Head.LocalId}, ability: {invoke.Head.InstancedAbilityId}");
				ability.DebugAbility(logger);
			}

			return;
		}

		//TODO add all cases
		switch (invoke.ArgumentType)
		{
			case AbilityInvokeArgument.AbilityNone:
				//TODO
				ConfigAbility ability = ConfigAbilityHashMap[InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]];
				if (ability.LocalIdToInvocationMap.TryGetValue((uint)invoke.Head.LocalId, out IInvocation? invocation))
				{
					Console.WriteLine($"Invoking ability: {ability.abilityName}, localId: {invoke.Head.LocalId}");
					await invocation.Invoke(ability.abilityName, Owner);
				}
				else
					logger.LogWarning($"Missing localId: {invoke.Head.LocalId}, ability: {invoke.Head.InstancedAbilityId}");
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
				AbilitySpecialOverrideMap[InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId]][asEntri.Key.Hash] = asEntri.FloatValue;
				break;
			case AbilityInvokeArgument.AbilityMetaReinitOverridemap:
				AbilityMetaReInitOverrideMap info3 = Serializer.Deserialize<AbilityMetaReInitOverrideMap>(data);
				ReInitOverrideMap(InstanceToAbilityHashMap[invoke.Head.InstancedAbilityId], info3 as AbilityMetaReInitOverrideMap);
				break;
			case AbilityInvokeArgument.AbilityMetaGlobalFloatValue:
				AbilityScalarValueEntry asEntry = Serializer.Deserialize<AbilityScalarValueEntry>(data);
				GlobalValueHashMap[asEntry.Key.Hash] = asEntry.FloatValue;
				break;
			case AbilityInvokeArgument.AbilityMetaAddOrGetAbilityAndTrigger:
				AbilityMetaAddOrGetAbilityAndTrigger info4 = Serializer.Deserialize<AbilityMetaAddOrGetAbilityAndTrigger>(data);
				break;
			case AbilityInvokeArgument.AbilityMetaAddNewAbility:
				AbilityMetaAddAbility info5 = Serializer.Deserialize<AbilityMetaAddAbility>(data);
				AddAbility(info5.Ability);
				break;
			case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
				AbilityMetaModifierDurabilityChange info6 = Serializer.Deserialize<AbilityMetaModifierDurabilityChange>(data);
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
			default:
				logger.LogWarning($"Unhandled AbilityInvokeArgument: {invoke.ArgumentType}");
				break;
		}
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
			$"ParentAbility={modifierChange.ParentAbilityName?.Hash:X}");

		try
		{
			// figure out who the modifier is applied to (you already do this)
			uint targetEntityId = modifierChange.ApplyEntityId != 0
				? modifierChange.ApplyEntityId
				: Owner._EntityId;

			uint instancedAbilityId = invoke.Head.InstancedAbilityId;
			uint instancedModifierId = invoke.Head.InstancedModifierId;

			if (!InstanceToAbilityHashMap.TryGetValue(instancedAbilityId, out uint abilityHash))
			{
				logger.LogWarning($"Missing ability hash for instancedAbilityId {instancedAbilityId}");
				return;
			}

			if (!ConfigAbilityHashMap.TryGetValue(abilityHash, out ConfigAbility? ability))
			{
				logger.LogWarning($"Missing ability config for ability hash {abilityHash}");
				return;
			}

			// get the modifier config for this ability, by local id  (0 in your example)
			// !!! adapt this line to how you actually store modifiers in ConfigAbility
			AbilityModifier? modifierConfig = null!;
			if (ability.ModifierList != null &&
				ability.ModifierList.TryGetValue((uint)modifierChange.ModifierLocalId, out var cfg))
			{
				modifierConfig = cfg;
			}
			else
			{
				logger.LogWarning($"No modifier config for LocalId={modifierChange.ModifierLocalId} in ability {ability.abilityName}");
				// you can return here or continue without modifierConfig
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
		catch (Exception ex)
		{
			logger.LogError($"Failed to apply modifier: {ex.Message}");
		}
	}

	protected virtual void ReInitOverrideMap(uint abilityNameHash, AbilityMetaReInitOverrideMap? overrideMap)
	{
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
						logger.LogWarning($"Unhandled value type {entry.ValueType} in Config {ConfigAbilityHashMap[hash].abilityName}");
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