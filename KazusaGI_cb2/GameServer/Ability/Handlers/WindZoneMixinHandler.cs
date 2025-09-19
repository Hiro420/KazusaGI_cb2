using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Ability.Handlers;

[AbilityMixin(typeof(WindZoneMixin))]
public class WindZoneMixinHandler : AbilityMixinHandler
{
	private static readonly Logger logger = new("WindZoneMixinHandler");

	public override async Task<bool> ExecuteAsync(
		ConfigAbility ability,
		BaseAbilityMixin mixin,
		byte[] abilityData,
		Entity source,
		Entity? target)
	{
		if (mixin is not WindZoneMixin windZoneMixin)
		{
			//logger.LogError("Mixin is not WindZoneMixin"); should never happen
			return false;
		}

		return true;
	}
}