using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Generic;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityLocalIdGenerator
{
    public enum ConfigAbilitySubContainerType : long
    {
        None = 0,
        Action = 1,
        Mixin = 2,
        ModifierAction = 3,
        ModifierMixin = 4
    }

    public ConfigAbilitySubContainerType Type { get; set; }
    public long ModifierIndex { get; set; } = 0;
    public long ConfigIndex { get; set; } = 0;
    public long MixinIndex { get; set; } = 0;
    private long actionIndex = 0;

    public AbilityLocalIdGenerator(ConfigAbilitySubContainerType type)
    {
        Type = type;
    }

    public void InitializeActionLocalIds(BaseAction[] actions, Dictionary<int, BaseAction> localIdToAction)
    {
        InitializeActionLocalIds(actions, localIdToAction, false);
    }

    public void InitializeActionLocalIds(
        BaseAction[] actions,
        Dictionary<int, BaseAction> localIdToAction,
        bool preserveActionIndex)
    {
        if (actions == null) return;
        if (!preserveActionIndex) actionIndex = 0;
        
        for (int i = 0; i < actions.Length; i++)
        {
            actionIndex++;

            var id = GetLocalId();
            localIdToAction[(int)id] = actions[i];

            // Handle nested actions based on action type
            var nestedActions = GetNestedActions(actions[i]);
            if (nestedActions != null)
            {
                InitializeActionLocalIds(nestedActions, localIdToAction, true);
            }
            else
            {
                var successActions = GetSuccessActions(actions[i]);
                if (successActions != null)
                {
                    InitializeActionLocalIds(successActions, localIdToAction, true);
                }
                
                var failActions = GetFailActions(actions[i]);
                if (failActions != null)
                {
                    InitializeActionLocalIds(failActions, localIdToAction, true);
                }
            }
        }

        if (!preserveActionIndex) actionIndex = 0;
    }

    public void InitializeMixinsLocalIds(BaseAbilityMixin[] mixins, Dictionary<int, BaseAbilityMixin> localIdToMixin)
    {
        if (mixins == null) return;
        MixinIndex = 0;
        
        foreach (var mixin in mixins)
        {
            var id = GetLocalId();
            localIdToMixin[(int)id] = mixin;
            MixinIndex++;
        }

        MixinIndex = 0;
    }

    public long GetLocalId()
    {
        return Type switch
        {
            ConfigAbilitySubContainerType.Action => (long)Type + (ConfigIndex << 3) + (actionIndex << 9),
            ConfigAbilitySubContainerType.Mixin => (long)Type + (MixinIndex << 3) + (ConfigIndex << 9) + (actionIndex << 15),
            ConfigAbilitySubContainerType.ModifierAction => (long)Type + (ModifierIndex << 3) + (ConfigIndex << 9) + (actionIndex << 15),
            ConfigAbilitySubContainerType.ModifierMixin => (long)Type + (ModifierIndex << 3) + (MixinIndex << 9) + (ConfigIndex << 15) + (actionIndex << 21),
            ConfigAbilitySubContainerType.None => throw new InvalidOperationException("Ability local id generator using NONE type."),
            _ => -1
        };
    }
    
    private BaseAction[] GetNestedActions(BaseAction action)
    {
        // This would need to be implemented based on your action structure
        // For now, return null as a placeholder
        return null;
    }
    
    private BaseAction[] GetSuccessActions(BaseAction action)
    {
        // This would need to be implemented based on your action structure
        // For now, return null as a placeholder
        return null;
    }
    
    private BaseAction[] GetFailActions(BaseAction action)
    {
        // This would need to be implemented based on your action structure
        // For now, return null as a placeholder
        return null;
    }
}