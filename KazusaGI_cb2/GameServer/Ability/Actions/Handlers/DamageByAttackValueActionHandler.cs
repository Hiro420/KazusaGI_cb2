using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;
using System;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability.Actions.Handlers;

public class DamageByAttackValueActionHandler : IAbilityActionHandler
{
    private static readonly Logger logger = new("DamageByAttackValueActionHandler");
    
    public Type GetActionType()
    {
        return typeof(DamageByAttackValue);
    }

    public async Task ExecuteAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target)
    {
        if (action is not DamageByAttackValue damageAction)
        {
            logger.LogError("Action is not DamageByAttackValue type");
            return;
        }

        logger.LogInfo($"Executing DamageByAttackValue action for entity {target._EntityId}");
        
        // TODO: Implement actual damage calculation and application
        logger.LogInfo($"DamageByAttackValue - Target: {damageAction.target}, AttackInfo: {damageAction.attackInfo}");
        
        await Task.CompletedTask;
    }
}