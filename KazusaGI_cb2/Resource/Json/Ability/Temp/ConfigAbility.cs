using System.Collections.Concurrent;
using Newtonsoft.Json;
using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public class ConfigAbility : BaseConfigAbility
{
    [JsonProperty] public readonly string abilityName;
    [JsonProperty] public readonly Dictionary<string, float>? abilitySpecials;
    [JsonProperty] public readonly BaseAbilityMixin[]? abilityMixins;
    [JsonProperty] public readonly Dictionary<string, AbilityModifier>? modifiers;
    [JsonProperty] public readonly BaseAction[]? onAdded;
    [JsonProperty] public readonly BaseAction[]? onRemoved;
    [JsonProperty] public readonly BaseAction[]? onAbilityStart;
    [JsonProperty] public readonly BaseAction[]? onKill;
    [JsonProperty] public readonly BaseAction[]? onFieldEnter;
    [JsonProperty] public readonly BaseAction[]? onExit;
    [JsonProperty] public readonly BaseAction[]? onAttach;
    [JsonProperty] public readonly BaseAction[]? onDetach;
    [JsonProperty] public readonly BaseAction[]? onAvatarIn;
    [JsonProperty] public readonly BaseAction[]? onAvatarOut;
    [JsonProperty] public readonly bool isDynamicAbility; // if true, disable this ability by default. Enable via ConfigTalent AddAbility     

    [JsonIgnore] public ConcurrentDictionary<uint, IInvocation> LocalIdToInvocationMap;
    [JsonIgnore] public SortedList<uint, AbilityModifier> ModifierList;

    internal async Task Initialize()
    {
        // DO NOT CHANGE THE ORDER
        LocalIdToInvocationMap = new();

        var tasks = new Task[]
        {
            InitializeMixinIds(),
            InitializeModifierIds(),
            InitializeActionIds()
        };
        await Task.WhenAll(tasks);
    }

    public void DebugAbility(Logger logger)
    {
		//Console.WriteLine(JsonConvert.SerializeObject(ModifierList, Formatting.Indented));
		foreach (var kvp in LocalIdToInvocationMap)
		{
			uint localId = kvp.Key;
			var (type, s3, s9, s15, s21) = LocalIdGenerator.DecodeLocalId(localId);

			switch (type)
			{
				case ConfigAbilitySubContainerType.ACTION:
					logger.LogWarning(
						$"LocalId {localId} -> Action: ConfigIndex={s3} [{ConfigIndexAction.GetValueOrDefault(s3, "?")}] | ActionIndex={s9} | Invocation={kvp.Value.GetType().Name}");
					break;

				case ConfigAbilitySubContainerType.MIXIN:
					logger.LogWarning(
						$"LocalId {localId} -> Mixin: MixinIndex={s3} | ConfigIndex={s9} [?] | ActionIndex={s15} | Invocation={kvp.Value.GetType().Name}");
					break;

				case ConfigAbilitySubContainerType.MODIFIER_ACTION:
					logger.LogWarning(
						$"LocalId {localId} -> ModifierAction: ModifierIndex={s3} | ConfigIndex={s9} [{ConfigIndexModifier.GetValueOrDefault(s9, "?")}] | ActionIndex={s15} | Invocation={kvp.Value.GetType().Name}");
					break;

				case ConfigAbilitySubContainerType.MODIFIER_MIXIN:
					logger.LogWarning(
						$"LocalId {localId} -> ModifierMixin: ModifierIndex={s3} | MixinIndex={s9} | ConfigIndex={s15} [?] | ActionIndex={s21} | Invocation={kvp.Value.GetType().Name}");
					break;

				default:
					logger.LogWarning(
						$"LocalId {localId} -> Unsupported type {(int)type}: s={(int)type}, s3={s3}, s9={s9}, s15={s15}, s21={s21}");
					break;
			}
		}
	}

    private async Task InitializeActionIds()
    {
        await Task.Yield();
        ushort configIndex = 0;
        LocalIdGenerator idGenerator = new(ConfigAbilitySubContainerType.ACTION);
        idGenerator.InitializeActionLocalIds(onAdded, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onRemoved, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onAbilityStart, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onKill, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onFieldEnter, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onExit, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onAttach, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onDetach, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onAvatarIn, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
        idGenerator.InitializeActionLocalIds(onAvatarOut, LocalIdToInvocationMap);
        idGenerator.ConfigIndex++;
    }

    private async Task InitializeMixinIds()
    {
        if (abilityMixins != null)
        {
            LocalIdGenerator idGenerator = new(ConfigAbilitySubContainerType.MIXIN);
            for (uint i = 0; i < abilityMixins.Length; i++)
            {
                idGenerator.ConfigIndex = 0;
                await abilityMixins[i].Initialize(idGenerator, LocalIdToInvocationMap);
                idGenerator.MixinIndex++;
            }
        }
    }

    private async Task InitializeModifierIds()
    {
        if (modifiers != null)
        {
            ModifierList = new();
            var modifierArray = modifiers.ToArray();
            var tasks = new Task[modifierArray.Length];
            ushort modifierIndex = 0;
            for (uint i = 0; i < modifierArray.Length; i++)
            {
                LocalIdGenerator idGenerator = new(ConfigAbilitySubContainerType.NONE) { ModifierIndex = modifierIndex };
                ModifierList[i] = modifierArray[i].Value;
                tasks[i] = modifierArray[i].Value.Initialize(idGenerator, LocalIdToInvocationMap);
                modifierIndex++;
            }

            await Task.WhenAll(tasks);
        }
    }

    private static readonly Dictionary<int, string> ConfigIndexAction = new()
    {
        [0] = "onAdded",
        [1] = "onRemoved",
        [2] = "onAbilityStart",
        [3] = "onKill",
        [4] = "onFieldEnter",
        [5] = "onFieldExit",
        [6] = "onAttach",
        [7] = "onDetach",
        [8] = "onAdded",
        [9] = "onAvatarIn",
        [10] = "onAvatarOut",
    };

    private static readonly Dictionary<int, string> ConfigIndexModifier = new()
    {
        [0] = "onAdded",
        [1] = "onRemoved",
        [2] = "onBeingHit",
        [3] = "onAttackLanded",
        [4] = "onHittingOther",
        [5] = "onThinkInterval",
        [6] = "onKill",
        [7] = "onCrash",
        [8] = "onAvatarIn",
        [9] = "onAvatarOut",
        [10] = "onReconnect",
        [11] = "onChangeAuthority",
        [12] = "onVehicleIn",
        [13] = "onVehicleOut",
        [14] = "onZoneEnter",
        [15] = "onZoneExit",
        [16] = "onHeal",
        [17] = "onBeingHealed"
    };
}
