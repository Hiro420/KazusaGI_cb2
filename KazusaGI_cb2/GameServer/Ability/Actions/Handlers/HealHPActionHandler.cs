using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;
using System;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability.Actions.Handlers;

public class HealHPActionHandler : IAbilityActionHandler
{
    private static readonly Logger logger = new("HealHPActionHandler");
    
    public Type GetActionType()
    {
        return typeof(HealHP);
    }

    public async Task ExecuteAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target)
    {
        if (action is not HealHP healAction)
        {
            logger.LogError("Action is not HealHP type");
            return;
        }

        logger.LogInfo($"Executing HealHP action for entity {target._EntityId}");
        
        // TODO: Implement actual healing logic
        // For now, just log that we received the action
        logger.LogInfo($"HealHP - Amount: {healAction.amount}, DoOffStage: {healAction.doOffStage}");
        
        await Task.CompletedTask;
    }
}