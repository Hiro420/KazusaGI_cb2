using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;
using System;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability.Actions.Handlers;

public class DoBlinkActionHandler : IAbilityActionHandler
{
    private static readonly Logger logger = new("DoBlinkActionHandler");
    
    public Type GetActionType()
    {
        return typeof(DoBlink);
    }

    public async Task ExecuteAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target)
    {
        if (action is not DoBlink blinkAction)
        {
            logger.LogError("Action is not DoBlink type");
            return;
        }

        logger.LogInfo($"Executing DoBlink action for entity {target._EntityId}");
        
        // TODO: Implement actual blink/teleport logic
        logger.LogInfo($"DoBlink - Distance: {blinkAction.Distance}, Direction: {blinkAction.Direction}, Target: {blinkAction.Target}");
        
        await Task.CompletedTask;
    }
}