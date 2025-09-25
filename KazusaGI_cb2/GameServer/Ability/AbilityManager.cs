using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Handlers;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability;

public sealed class AbilityManager
{
    private static Logger logger = new("AbilityManager");
    private static readonly Dictionary<Type, IAbilityActionHandler> actionHandlers = new();
    private static readonly Dictionary<Type, AbilityMixinHandler> mixinHandlers = new();
    
    public static readonly SemaphoreSlim eventSemaphore = new(4, 4);
    
    private readonly Player player;
    public bool AbilityInvulnerable { get; private set; } = false;
    private int burstCasterId;
    private int burstSkillId;
    
    static AbilityManager()
    {
        RegisterHandlers();
    }
    
    public AbilityManager(Player player)
    {
        this.player = player;
        RemovePendingEnergyClear();
    }
    
    public void RemovePendingEnergyClear()
    {
        burstCasterId = 0;
        burstSkillId = 0;
    }
    
    private void OnPossibleElementalBurst(Ability ability, AbilityModifier modifier, int entityId)
    {
        // Possibly clear avatar energy spent on elemental burst
        // and set invulnerability.
        //
        // Problem: Burst can misfire occasionally, like hitting Q when
        // dashing, doing E, or switching avatars. The client would
        // still send EvtDoSkillSuccNotify, but the burst may not
        // actually happen. We don't know when to clear avatar energy.
        //
        // When burst does happen, a number of AbilityInvokeEntry will
        // come in. Use the Ability it references and search for any
        // modifier with type=AvatarSkillStart, skillID=burst skill ID.
        //
        // If that is missing, search for modifier action that sets
        // invulnerability as a fallback.
        
        if (burstCasterId == 0) return;
        
        bool skillInvincibility = false;
        if (modifier.onAdded != null)
        {
            foreach (var action in modifier.onAdded)
            {
                // Check for actions that indicate invulnerability
                var actionTypeName = action.GetType().Name;
                if (actionTypeName == "SetGlobalValue" || actionTypeName == "SetInvincible" || 
                    actionTypeName.Contains("Invincible"))
                {
                    skillInvincibility = true;
                    break;
                }
            }
        }
        
        if (burstCasterId == entityId && 
           (ability.AvatarSkillStartIds.Contains(burstSkillId) || skillInvincibility))
        {
            var currentAvatar = player.TeamManager.GetCurrentAvatarEntity();
            if (currentAvatar != null && currentAvatar._EntityId == entityId)
            {
                //currentAvatar.ClearEnergy();
                RemovePendingEnergyClear();
            }
            
            if (skillInvincibility)
            {
                AbilityInvulnerable = true;
            }
        }
    }
    
    public static void RegisterHandlers()
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        // Register action handlers
        var handlerClassesAction = assembly.GetTypes()
            .Where(t => typeof(IAbilityActionHandler).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);
        
        logger.LogInfo($"Found {handlerClassesAction.Count()} action handler classes");
            
        foreach (var handlerClass in handlerClassesAction)
        {
            try
            {
                var handler = (IAbilityActionHandler)Activator.CreateInstance(handlerClass)!;
                var actionType = handler.GetActionType();
                if (actionType != null)
                {
                    actionHandlers[actionType] = handler;
                    logger.LogInfo($"Registered action handler: {handlerClass.Name} for type: {actionType.Name}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to register action handler {handlerClass.Name}: {ex.Message}");
            }
        }
        
        // Register mixin handlers
        var handlerClassesMixin = assembly.GetTypes()
            .Where(t => typeof(AbilityMixinHandler).IsAssignableFrom(t) && !t.IsAbstract);
        
        logger.LogInfo($"Found {handlerClassesMixin.Count()} mixin handler classes");
            
        foreach (var handlerClass in handlerClassesMixin)
        {
            try
            {
                var attribute = handlerClass.GetCustomAttribute<AbilityMixinAttribute>();
                if (attribute != null)
                {
                    var handler = (AbilityMixinHandler)Activator.CreateInstance(handlerClass)!;
                    mixinHandlers[attribute.MixinType] = handler;
                    logger.LogInfo($"Registered mixin handler: {handlerClass.Name} for type: {attribute.MixinType.Name}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to register mixin handler {handlerClass.Name}: {ex.Message}");
            }
        }
        
        logger.LogInfo($"Total handlers registered: {actionHandlers.Count} actions, {mixinHandlers.Count} mixins");
    }
    
    public async Task ExecuteActionAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target)
    {
        var actionType = action.GetType();
        logger.LogInfo($"Attempting to execute action: {actionType.Name}");
        logger.LogInfo($"Available action handlers: {string.Join(", ", actionHandlers.Keys.Select(k => k.Name))}");
        
        if (!actionHandlers.TryGetValue(actionType, out var handler) || ability == null)
        {
            logger.LogWarning($"No handler found for action type: {actionType.Name}");
            
            // Try to find a generic handler as fallback
            if (actionHandlers.TryGetValue(typeof(BaseAction), out var genericHandler))
            {
                logger.LogInfo("Using generic action handler as fallback");
                handler = genericHandler;
            }
            else
            {
                return;
            }
        }
        
        await eventSemaphore.WaitAsync();
        try
        {
            await Task.Run(async () =>
            {
                try
                {
                    await handler.ExecuteAsync(ability, action, abilityData, target);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error executing action {actionType.Name}: {ex.Message}");
                }
            });
        }
        finally
        {
            eventSemaphore.Release();
        }
    }
    
    public async Task ExecuteMixinAsync(Ability ability, BaseAbilityMixin mixinData, byte[] abilityData)
    {
        var mixinType = mixinData.GetType();
        logger.LogInfo($"Attempting to execute mixin: {mixinType.Name}");
        logger.LogInfo($"Available mixin handlers: {string.Join(", ", mixinHandlers.Keys.Select(k => k.Name))}");
        
        if (!mixinHandlers.TryGetValue(mixinType, out var handler) || ability == null)
        {
            logger.LogWarning($"No handler found for mixin type: {mixinType.Name}");
            return;
        }
        
        await eventSemaphore.WaitAsync();
        try
        {
            await Task.Run(async () =>
            {
                try
                {
                    await handler.ExecuteAsync(ability.Data, mixinData, abilityData, ability.Owner, null);
                }
                catch (Exception ex)
                {
                    logger.LogError($"Error executing mixin {mixinType.Name}: {ex.Message}");
                }
            });
        }
        finally
        {
            eventSemaphore.Release();
        }
    }
    
    public async Task OnAbilityInvokeAsync(AbilityInvokeEntry invoke)
    {
        logger.LogInfo($"Ability invoke: {invoke} {invoke.ArgumentType} ({(int)invoke.ArgumentType}): " +
                       $"{player.Scene.FindEntityByEntityId(invoke.EntityId)}");
                       
        var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
        if (entity != null)
        {
            // Handle entity-specific ability invokes
            if (entity.InstancedAbilities.Count > invoke.Head.InstancedAbilityId - 1 && invoke.Head.InstancedAbilityId > 0)
            {
                var ability = entity.InstancedAbilities[(int)(invoke.Head.InstancedAbilityId - 1)];
                // Process ability-specific logic here
            }
        }
        else
        {
            logger.LogWarning($"Entity not found for ability invoke: {invoke.EntityId}");
        }
        
        if (invoke.Head.TargetId != 0)
        {
            var target = player.Scene.FindEntityByEntityId(invoke.Head.TargetId);
            // Handle target entity logic
        }

        if (invoke.Head.LocalId != 0)
        {
            // Handle local ID logic
            
        }
        
        switch (invoke.ArgumentType)
        {
            case AbilityInvokeArgument.AbilityMetaOverrideParam:
                HandleOverrideParam(invoke);
                break;
            case AbilityInvokeArgument.AbilityMetaReinitOverridemap:
                HandleReinitOverrideMap(invoke);
                break;
            case AbilityInvokeArgument.AbilityMetaModifierChange:
                HandleModifierChange(invoke);
                break;
            case AbilityInvokeArgument.AbilityMixinCostStamina:
                HandleMixinCostStamina(invoke);
                break;
            case AbilityInvokeArgument.AbilityActionGenerateElemBall:
                HandleGenerateElemBall(invoke);
                break;
            case AbilityInvokeArgument.AbilityMetaGlobalFloatValue:
                HandleGlobalFloatValue(invoke);
                break;
            case AbilityInvokeArgument.AbilityMetaModifierDurabilityChange:
                HandleModifierDurabilityChange(invoke);
                break;
            case AbilityInvokeArgument.AbilityMetaAddNewAbility:
                HandleAddNewAbility(invoke);
                break;
            case AbilityInvokeArgument.AbilityMetaSetKilledSetate:
                HandleKillState(invoke);
                break;
            default:
                logger.LogWarning($"Unhandled ability invoke argument type: {invoke.ArgumentType}");
                break;
        }
    }
    
    public void HandleServerInvoke(AbilityInvokeEntry invoke)
    {
        var head = invoke.Head;
        var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
        
        if (entity == null)
        {
            logger.LogWarning($"Entity not found for server invoke: {invoke.EntityId}");
            return;
        }
        
        var target = player.Scene.FindEntityByEntityId(head.TargetId);
        if (target == null && head.TargetId != 0)
        {
            logger.LogWarning($"Target entity not found: {head.TargetId}");
        }
        
        Ability ability = null;
        
        // Find ability or modifier's ability
        if (head.InstancedModifierId != 0 && 
            entity.InstancedModifiers.TryGetValue((int)head.InstancedModifierId, out var modifier))
        {
            ability = modifier.Ability;
        }
        
        if (ability == null && head.InstancedAbilityId != 0 && 
            (head.InstancedAbilityId - 1) < entity.InstancedAbilities.Count)
        {
            ability = entity.InstancedAbilities[(int)(head.InstancedAbilityId - 1)];
        }
        
        if (ability == null)
        {
            logger.LogWarning($"Ability not found for server invoke. InstancedAbilityId: {head.InstancedAbilityId}, " +
                            $"InstancedModifierId: {head.InstancedModifierId}");
            return;
        }
        
        // Debug logging
        logger.LogInfo($"Server invoke: local_id {head.LocalId}, ability {ability.Data.abilityName}");
        logger.LogInfo($"LocalIdToInvocationMap null: {ability.Data.LocalIdToInvocationMap == null}");
        if (ability.Data.LocalIdToInvocationMap != null)
        {
            logger.LogInfo($"LocalIdToInvocationMap keys: {string.Join(",", ability.Data.LocalIdToInvocationMap.Keys)}");
        }
        logger.LogInfo($"LocalIdToAction keys: {string.Join(",", ability.Data.LocalIdToAction?.Keys)}");
        
        // Time to reach the handlers
        if (ability.Data.LocalIdToInvocationMap != null && 
            ability.Data.LocalIdToInvocationMap.TryGetValue((uint)head.LocalId, out var invocation))
        {
            logger.LogInfo($"Found invocation for local_id {head.LocalId}: {invocation.GetType().Name}");
            if (invocation is BaseAction action)
            {
                logger.LogInfo($"Executing action: {action.GetType().Name}");
                _ = Task.Run(() => ExecuteActionAsync(ability, action, invoke.AbilityData, target ?? entity));
            }
            else if (invocation is BaseAbilityMixin mixin)
            {
                logger.LogInfo($"Executing mixin: {mixin.GetType().Name}");
                _ = Task.Run(() => ExecuteMixinAsync(ability, mixin, invoke.AbilityData));
            }
        }
        else
        {
            logger.LogWarning($"Action or mixin not found: local_id {head.LocalId} ability {ability.Data.abilityName} " +
                           $"LocalIdToAction keys: {string.Join(",", ability.Data.LocalIdToAction?.Keys)}");
        }
    }
    
    public void OnSkillStart(Player player, int skillId, int casterId)
    {
        // Check if the player matches this player
        if (player.Uid != this.player.Uid)
        {
            return;
        }
        
        // Check if the caster matches the player
        var currentAvatar = player.TeamManager.GetCurrentAvatarEntity();
        if (currentAvatar == null || currentAvatar._EntityId != casterId)
        {
            return;
        }
        
        var skillData = MainApp.resourceManager.AvatarSkillExcel.GetValueOrDefault((uint)skillId);
        if (skillData == null)
        {
            return;
        }
        
        // Invoke PlayerUseSkillEvent
        var eventArgs = new PlayerUseSkillEventArgs(player, skillData, currentAvatar);
        // Event handling would go here
        
        // Check if the skill is an elemental burst
        if (skillData.costElemVal <= 0)
        {
            return;
        }
        
        // Track this elemental burst to possibly clear avatar energy later
        burstCasterId = casterId;
        burstSkillId = skillId;
    }
    
    public void OnSkillEnd(Player player)
    {
        // Check if the player matches this player
        if (player.Uid != this.player.Uid)
        {
            return;
        }
        
        // Clear invulnerability state
        AbilityInvulnerable = false;
        RemovePendingEnergyClear();
    }
    
    private void SetAbilityOverrideValue(Ability ability, AbilityScalarValueEntry valueChange)
    {
        if (ability.Data.abilitySpecials != null && 
            ability.Data.abilitySpecials.TryGetValue(valueChange.Key.Str ?? string.Empty, out _))
        {
            ability.AbilitySpecials[valueChange.Key.Str ?? string.Empty] = valueChange.FloatValue;
        }
    }
    
    private void HandleOverrideParam(AbilityInvokeEntry invoke)
    {
        try
        {
            var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
            if (entity == null) return;
            
            var ability = entity.InstancedAbilities.ElementAtOrDefault((int)(invoke.Head.InstancedAbilityId - 1));
            if (ability == null) return;
            
            var valueChange = ProtoBuf.Serializer.Deserialize<AbilityScalarValueEntry>(
                new MemoryStream(invoke.AbilityData));
            SetAbilityOverrideValue(ability, valueChange);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error handling override param: {ex.Message}");
        }
    }
    
    private void HandleReinitOverrideMap(AbilityInvokeEntry invoke)
    {
        try
        {
            var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
            if (entity == null) return;
            
            var ability = entity.InstancedAbilities.ElementAtOrDefault((int)(invoke.Head.InstancedAbilityId - 1));
            if (ability == null) return;
            
            var overrideMap = ProtoBuf.Serializer.Deserialize<AbilityMetaReInitOverrideMap>(
                new MemoryStream(invoke.AbilityData));
                
            foreach (var entry in overrideMap.OverrideMaps)
            {
                SetAbilityOverrideValue(ability, entry);
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error handling reinit override map: {ex.Message}");
        }
    }
    
    private void HandleModifierChange(AbilityInvokeEntry invoke)
    {
        try
        {
            var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
            if (entity == null) return;
            
            var modifierChange = ProtoBuf.Serializer.Deserialize<AbilityMetaModifierChange>(
                new MemoryStream(invoke.AbilityData));
                
            var ability = entity.InstancedAbilities.ElementAtOrDefault((int)(invoke.Head.InstancedAbilityId - 1));
            if (ability?.Data.modifiers == null) return;
            
            var modifierName = modifierChange.ParentAbilityName?.Str ?? string.Empty;
            if (string.IsNullOrEmpty(modifierName) || 
                !ability.Data.modifiers.TryGetValue(modifierName, out var modifier))
                return;
            
            switch (modifierChange.Action)
            {
                case ModifierAction.Added:
                    OnPossibleElementalBurst(ability, modifier, (int)invoke.EntityId);
                    break;
                case ModifierAction.Removed:
                    // Handle modifier removal
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error handling modifier change: {ex.Message}");
        }
    }
    
    private void HandleMixinCostStamina(AbilityInvokeEntry invoke)
    {
        // Handle stamina cost mixin
    }
    
    private void HandleGenerateElemBall(AbilityInvokeEntry invoke)
    {
        // Handle elemental ball generation
    }
    
    private void HandleGlobalFloatValue(AbilityInvokeEntry invoke)
    {
        try
        {
            var floatValue = ProtoBuf.Serializer.Deserialize<AbilityScalarValueEntry>(
                new MemoryStream(invoke.AbilityData));
                
            // Store global float value for later use
            var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
            if (entity?.abilityManager != null)
            {
                // Store in entity's ability manager global values
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error handling global float value: {ex.Message}");
        }
    }
    
    private void HandleModifierDurabilityChange(AbilityInvokeEntry invoke)
    {
        // Handle modifier durability changes
    }
    
    private void HandleAddNewAbility(AbilityInvokeEntry invoke)
    {
        try
        {
            var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
            if (entity == null) return;
            
            var addAbility = ProtoBuf.Serializer.Deserialize<AbilityMetaAddAbility>(
                new MemoryStream(invoke.AbilityData));
                
            AddAbilityToEntity(entity, addAbility.Ability.AbilityName.Str ?? string.Empty);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error handling add new ability: {ex.Message}");
        }
    }
    
    private void HandleKillState(AbilityInvokeEntry invoke)
    {
        try
        {
            var entity = player.Scene.FindEntityByEntityId(invoke.EntityId);
            if (entity == null) return;
            
            var killState = ProtoBuf.Serializer.Deserialize<AbilityMetaSetKilledState>(
                new MemoryStream(invoke.AbilityData));
                
            if (killState.Killed)
            {
                entity.ForceKill();
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error handling kill state: {ex.Message}");
        }
    }
    
    public void AddAbilityToEntity(Entity entity, string name)
    {
        var abilityData = MainApp.resourceManager.GetAbilityData(name);
        if (abilityData != null)
        {
            AddAbilityToEntity(entity, abilityData);
        }
    }
    
    public void AddAbilityToEntity(Entity entity, ConfigAbility abilityData)
    {
        var ability = new Ability(abilityData, entity, player);
        entity.InstancedAbilities.Add(ability);
    }
}

// Supporting interfaces and classes
public interface IAbilityActionHandler
{
    Type GetActionType();
    Task ExecuteAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target);
}

public class PlayerUseSkillEventArgs : EventArgs
{
    public Player Player { get; }
    public AvatarSkillExcelConfig SkillData { get; }
    public AvatarEntity Avatar { get; }
    
    public PlayerUseSkillEventArgs(Player player, AvatarSkillExcelConfig skillData, AvatarEntity avatar)
    {
        Player = player;
        SkillData = skillData;
        Avatar = avatar;
    }
}