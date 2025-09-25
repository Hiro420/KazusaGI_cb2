using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability.Actions.Handlers;

/// <summary>
/// Generic fallback handler for debugging unknown actions
/// </summary>
public class GenericActionHandler : IAbilityActionHandler
{
    private static readonly Logger logger = new("GenericActionHandler");
    
    public Type GetActionType()
    {
        return typeof(BaseAction);
    }

    public async Task ExecuteAsync(Ability ability, BaseAction action, byte[] abilityData, Entity target)
    {
        logger.LogInfo($"Executing generic action: {action.GetType().Name} for entity {target._EntityId}");
        logger.LogInfo($"Action details: Type={action.GetType().FullName}, Ability={ability.Data.abilityName}");
        
        await Task.CompletedTask;
    }
}