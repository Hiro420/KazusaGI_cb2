using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KazusaGI_cb2.GameServer.Ability;

public class Ability
{
    public ConfigAbility Data { get; private set; }
    public Entity Owner { get; private set; }
    public Player PlayerOwner { get; private set; }
    public AbilityManager Manager { get; private set; }
    
    public Dictionary<string, AbilityModifierController> Modifiers { get; private set; } = new();
    public Dictionary<string, float> AbilitySpecials { get; private set; } = new();
    public static Dictionary<string, Dictionary<string, float>> AbilitySpecialsModified { get; private set; } = new();
    
    public uint Hash { get; private set; }
    public HashSet<int> AvatarSkillStartIds { get; private set; } = new();
    
    public Ability(ConfigAbility data, Entity owner, Player playerOwner)
    {
        Data = data;
        Owner = owner;
        PlayerOwner = playerOwner;
        // Manager = playerOwner.session.AbilityManager ?? new AbilityManager(playerOwner);
        
        if (Data.abilitySpecials != null)
        {
            foreach (var entry in Data.abilitySpecials)
            {
                AbilitySpecials[entry.Key] = entry.Value;
            }
        }
        
        Hash = Utils.AbilityHash(data.abilityName);
        
        // Initialize the data synchronously to ensure LocalIdToInvocationMap is populated
        data.Initialize().GetAwaiter().GetResult();
        
        // Collect skill IDs referenced by AvatarSkillStart modifier actions
        // in onAbilityStart and in every modifier's onAdded action set.
        // These skill IDs will be used by AbilityManager to determine whether
        // an elemental burst has fired correctly.
        if (data.onAbilityStart != null)
        {
            AvatarSkillStartIds.UnionWith(
                data.onAbilityStart
                    .Where(action => action.GetType().Name.Contains("AvatarSkillStart"))
                    .Select(action => GetSkillIdFromAction(action))
                    .Where(skillId => skillId != 0)
            );
        }
        
        if (data.modifiers != null)
        {
            foreach (var modifier in data.modifiers.Values)
            {
                if (modifier.onAdded != null)
                {
                    AvatarSkillStartIds.UnionWith(
                        modifier.onAdded
                            .Where(action => action.GetType().Name.Contains("AvatarSkillStart"))
                            .Select(action => GetSkillIdFromAction(action))
                            .Where(skillId => skillId != 0)
                    );
                }
            }
        }
        
        if (data.onAdded != null)
        {
            ProcessOnAddedAbilityModifiers();
        }
    }
    
    private int GetSkillIdFromAction(BaseAction action)
    {
        // This would need to be implemented based on your action structure
        // For now, return 0 as a placeholder
        return 0;
    }
    
    public void ProcessOnAddedAbilityModifiers()
    {
        if (Data.onAdded == null) return;
        
        foreach (var modifierAction in Data.onAdded)
        {
            if (modifierAction == null) continue;
            
            if (modifierAction.GetType().Name.Contains("ApplyModifier"))
            {
                var modifierName = GetModifierNameFromAction(modifierAction);
                if (string.IsNullOrEmpty(modifierName)) continue;
                
                if (Data.modifiers != null && Data.modifiers.TryGetValue(modifierName, out var modifierData))
                {
                    // Handle modifier addition - would need to implement OnAddAbilityModifier in Entity
                }
            }
        }
    }
    
    private string GetModifierNameFromAction(BaseAction action)
    {
        // This would need to be implemented based on your action structure
        // For now, return empty string as a placeholder
        return string.Empty;
    }
    
    public static string GetAbilityName(AbilityString abString)
    {
        if (!string.IsNullOrEmpty(abString.Str))
            return abString.Str;
            
        if (abString.Hash != 0)
        {
            // This would need access to ability hashes map - simplified for now
            return $"Ability_{abString.Hash}";
        }
        
        return null;
    }
    
    public override string ToString()
    {
        return $"Ability Name: {Data.abilityName}; Entity Owner: {Owner}; Player Owner: {PlayerOwner}";
    }
}