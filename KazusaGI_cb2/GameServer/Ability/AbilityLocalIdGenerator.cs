using KazusaGI_cb2.Resource.Json.Ability.Temp;
namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityLocalIdGenerator
{
    private Logger logger = new("AbilityLocalIdGenerator");
    public enum ConfigAbilitySubContainerType : int
    {
        NONE = 0,
        ACTION = 1,
        MIXIN = 2,
        MODIFIER_ACTION = 3,
        MODIFIER_MIXIN = 4
    }

    public ConfigAbilitySubContainerType Type { get; set; }
    public uint ModifierIndex { get; set; } = 0;
    public uint ConfigIndex { get; set; } = 0;
    public uint MixinIndex { get; set; } = 0;
    private uint actionIndex = 0;

    public AbilityLocalIdGenerator(ConfigAbilitySubContainerType type)
    {
        this.Type = type;
    }

    private static uint _nextInstancedModifierId = 1;

    public static uint GenerateInstancedModifierId()
    {
        return System.Threading.Interlocked.Increment(ref _nextInstancedModifierId);
    }

    public void InitializeActionLocalIds(BaseAction[]? actions, IDictionary<uint, IInvocation> localIdToInvocationMap)
    {
        InitializeActionLocalIds(actions, localIdToInvocationMap, false);
    }

    public void InitializeActionLocalIds(BaseAction[]? actions, IDictionary<uint, IInvocation> localIdToInvocationMap, bool preserveActionIndex)
    {
        if (actions == null) return;
        if (!preserveActionIndex) actionIndex = 0;

        for (int i = 0; i < actions.Length; i++)
        {
            actionIndex++;
            var id = (uint)GetLocalId();
            // Map id -> invocation representation
            if (!localIdToInvocationMap.ContainsKey(id))
                localIdToInvocationMap[id] = actions[i];

            // nested actions
            if (actions[i].actions != null)
            {
                InitializeActionLocalIds(actions[i].actions, localIdToInvocationMap, true);
            }
            else
            {
                if (actions[i].successActions != null)
                    InitializeActionLocalIds(actions[i].successActions, localIdToInvocationMap, true);
                if (actions[i].failActions != null)
                    InitializeActionLocalIds(actions[i].failActions, localIdToInvocationMap, true);
            }
        }

        if (!preserveActionIndex) actionIndex = 0;
    }

    public void InitializeMixinsLocalIds(BaseAbilityMixin[]? mixins, IDictionary<uint, IInvocation> localIdToInvocationMap)
    {
        if (mixins == null) return;
        MixinIndex = 0;
        for (int i = 0; i < mixins.Length; i++)
        {
            var id = (uint)GetLocalId();
            if (!localIdToInvocationMap.ContainsKey(id))
                localIdToInvocationMap[id] = mixins[i];
            MixinIndex++;
        }
        MixinIndex = 0;
    }

    public long GetLocalId()
    {
        switch (Type)
        {
            case ConfigAbilitySubContainerType.ACTION:
                return (long)Type + (ConfigIndex << 3) + (actionIndex << 9);
            case ConfigAbilitySubContainerType.MIXIN:
                return (long)Type + (MixinIndex << 3) + (ConfigIndex << 9) + (actionIndex << 15);
            case ConfigAbilitySubContainerType.MODIFIER_ACTION:
                return (long)Type + (ModifierIndex << 3) + (ConfigIndex << 9) + (actionIndex << 15);
            case ConfigAbilitySubContainerType.MODIFIER_MIXIN:
                return (long)Type + (ModifierIndex << 3) + (MixinIndex << 9) + (ConfigIndex << 15) + (actionIndex << 21);
            case ConfigAbilitySubContainerType.NONE:
            default:
                logger.LogError("Ability local id generator using NONE type.");
                break;
        }
        return -1;
    }
}
