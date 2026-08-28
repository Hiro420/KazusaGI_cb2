using KazusaGI_cb2.Resource.Json.Ability.Temp;

namespace KazusaGI_cb2.GameServer.Ability;

public class AbilityInstance
{
	public AbilityInstance(ConfigAbility data, Entity entity, Player player)
	{
		Data = data;
		Entity = entity;
		Player = player;
	}

	public ConfigAbility Data { get; }
	public Entity Entity { get; }
	public Player Player { get; }
}
