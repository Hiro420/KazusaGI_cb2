using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.Actions;
using System;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability.Actions.Handlers;

public class GenerateElemBallActionHandler : IAbilityActionHandler
{
    private static readonly Logger logger = new("GenerateElemBallActionHandler");
    
    public Type GetActionType()
    {
        return typeof(GenerateElemBall);
    }

    public async Task ExecuteAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target)
    {
        if (action is not GenerateElemBall generateAction)
        {
            logger.LogError("Action is not GenerateElemBall type");
            return;
        }

        logger.LogInfo($"Executing GenerateElemBall action for entity {target._EntityId}");
        
        // TODO: Implement actual elem ball generation logic
        logger.LogInfo($"GenerateElemBall - ConfigID: {generateAction.configID}, Ratio: {generateAction.ratio}, BaseEnergy: {generateAction.baseEnergy}");
        
        await Task.CompletedTask;
    }
}