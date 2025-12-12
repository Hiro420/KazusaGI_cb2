using KazusaGI_cb2.Resource;

namespace KazusaGI_cb2.Resource.Json.Ability.Temp;

public class LocalIdGenerator
{
    private static Logger logger = new("LocalIdGenerator");
    public ConfigAbilitySubContainerType Type = ConfigAbilitySubContainerType.NONE;
    public uint ModifierIndex = 0;
    public uint ConfigIndex = 0;
    public uint MixinIndex = 0;
    private uint ActionIndex = 0;

    public LocalIdGenerator(ConfigAbilitySubContainerType type)
    {
        Type = type;
    }

    public void InitializeActionLocalIds(
        BaseAction[]? actions,
        IDictionary<uint, IInvocation> localIdToInvocationMap,
        IList<IInvocation> invokeSiteList)
    {
        if (actions == null) return;
        ActionIndex = 0;
        for (ushort i = 0; i < actions.Length; i++)
        {
            ActionIndex++;
            uint id = GetLocalId();

            // Keep the old bit-packed id mapping for debugging and
            // modifier-related lookups.
            localIdToInvocationMap.Add(id, actions[i]);

            // Also append to the sequential invoke-site list so that
            // head.LocalId can be used as an index into this list.
            invokeSiteList.Add(actions[i]);
        }
        ActionIndex = 0;
    }

    public uint GetLocalId()
    {
        switch (Type)
        {
            case ConfigAbilitySubContainerType.ACTION:
                return (uint)Type + (ConfigIndex << 3) + (ActionIndex << 9);
            case ConfigAbilitySubContainerType.MIXIN:
                return (uint)Type + (MixinIndex << 3) + (ConfigIndex << 9) + (ActionIndex << 15);
            case ConfigAbilitySubContainerType.MODIFIER_ACTION:
                return (uint)Type + (ModifierIndex << 3) + (ConfigIndex << 9) + (ActionIndex << 15);
            case ConfigAbilitySubContainerType.MODIFIER_MIXIN:
                return (uint)Type + (ModifierIndex << 3) + (MixinIndex << 9) + (ConfigIndex << 15) + (ActionIndex << 21);
            default:
				logger.LogWarning("Invalid ConfigAbilitySubContainerType");
                return 0;
        }
    }

    public static (ConfigAbilitySubContainerType type, int s3, int s9, int s15, int s21)
        DecodeLocalId(uint localId)
    {
        int s21 = (int)(localId >> 21);
        int s15 = (int)((localId - ((uint)s21 << 21)) >> 15);
        int s9 = (int)((localId - ((uint)s21 << 21) - ((uint)s15 << 15)) >> 9);
        int s3 = (int)((localId - ((uint)s21 << 21) - ((uint)s15 << 15) - ((uint)s9 << 9)) >> 3);
        int s = (int)(localId - ((uint)s21 << 21) - ((uint)s15 << 15) - ((uint)s9 << 9) - ((uint)s3 << 3));

        return ((ConfigAbilitySubContainerType)s, s3, s9, s15, s21);
    }
}
