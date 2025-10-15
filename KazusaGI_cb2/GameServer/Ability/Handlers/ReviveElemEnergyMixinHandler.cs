using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;

namespace KazusaGI_cb2.GameServer.Ability.Handlers;


[AbilityMixin(typeof(ReviveElemEnergyMixin))]
public class ReviveElemEnergyMixinHandler : AbilityMixinHandler
{
    private static readonly Logger logger = new("ReviveElemEnergyMixinHandler");
    
    public override async Task<bool> ExecuteAsync(
        ConfigAbility ability, 
        BaseAbilityMixin mixin, 
        Entity source, 
        Entity? target)
    {
        if (mixin is not ReviveElemEnergyMixin reviveElemEnergyMixin)
        {
			//logger.LogError("Mixin is not ReviveElemEnergyMixin"); // should never happen
			return false;
        }

        // TODO

        return true;
    }
}
