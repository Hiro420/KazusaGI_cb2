using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

namespace KazusaGI_cb2.GameServer.Ability.Handlers;

[AbilityMixin(typeof(AttachToStateIDMixin))]
public class AttachToStateIDMixinHandler : AbilityMixinHandler
{
    private static readonly Logger logger = new("AttachToStateIDMixinHandler");
    
    public override async Task<bool> ExecuteAsync(
        ConfigAbility ability, 
        BaseAbilityMixin mixin, 
        byte[] abilityData, 
        Entity source, 
        Entity? target)
    {
        if (mixin is not AttachToStateIDMixin attachToStateIDMixin)
        {
            logger.LogError("Mixin is not AttachToStateIDMixin");
            return false;
        }

        try
        {
            // Log the execution (similar to the GC warning log)
            logger.LogWarning($"AttachToStateIDMixin CALL on {attachToStateIDMixin.modifierName}");

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError($"Error executing AttachToStateIDMixin: {ex.Message}");
            return false;
        }
    }
}