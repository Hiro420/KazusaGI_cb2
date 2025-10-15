using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

namespace KazusaGI_cb2.GameServer.Ability.Handlers;


[AbilityMixin(typeof(CostStaminaMixin))]
public class CostStaminaMixinHandler : AbilityMixinHandler
{
    private static readonly Logger logger = new("CostStaminaMixinHandler");
    
    public override async Task<bool> ExecuteAsync(
        ConfigAbility ability, 
        BaseAbilityMixin mixin, 
        Entity source, 
        Entity? target)
    {
        if (mixin is not CostStaminaMixin costStaminaMixin)
        {
            //logger.LogError("Mixin is not CostStaminaMixin"); // should never happen
            return false;
        }

        // todo: handle
        return true;
    }
}
