using System.IO;
using KazusaGI_cb2.GameServer;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using KazusaGI_cb2.Resource.Json.Ability.Temp.AbilityMixins;
using ProtoBuf;

namespace KazusaGI_cb2.GameServer.Ability.Handlers;

[AbilityMixin(typeof(ShieldBarMixin))]
public class ShieldBarMixinHandler : AbilityMixinHandler
{
	private static readonly Logger logger = new("ShieldBarMixinHandler");

	public override async Task<bool> ExecuteAsync(
		ConfigAbility ability,
		BaseAbilityMixin mixin,
		byte[] abilityData,
		Entity source,
		Entity? target)
	{
		if (mixin is not ShieldBarMixin)
		{
			return false;
		}

		if (abilityData == null || abilityData.Length == 0)
		{
			return false;
		}

		try
		{
			using MemoryStream ms = new(abilityData);
			AbilityMixinShieldBar info = Serializer.Deserialize<AbilityMixinShieldBar>(ms);

			// Persist the latest shield bar values on the owning entity so
			// other systems (logging, future combat logic, Lua, etc.) can
			// inspect them if needed.
			source.UpdateShieldBar(info.ElementType, info.Shield, info.MaxShield);

			logger.LogInfo($"ShieldBar update on entity {source._EntityId}: elementType={info.ElementType}, shield={info.Shield}, maxShield={info.MaxShield}");
		}
		catch (Exception ex)
		{
			logger.LogError($"Error executing ShieldBarMixin: {ex.Message}");
			return false;
		}

		await Task.Yield();
		return true;
	}
}
