using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using System.Numerics;

namespace KazusaGI_cb2.GameServer;

public class TeamEntity : Entity
{
	public TeamAbilityManager AbilityManager => (TeamAbilityManager)abilityManager!;

	public TeamEntity(Session session, Vector3? position = null, Vector3? rotation = null)
		: base(session, position, rotation, ProtEntityType.ProtEntityTeam)
	{
		abilityManager = new TeamAbilityManager(this);
		abilityManager.Initialize();
	}

	protected override void BuildKindSpecific(SceneEntityInfo info)
	{
		// Currently no need
	}

	// Override ForceKill to prevent TeamEntity from being removed from entityMap
	public override void ForceKill()
	{
		// Do nothing to prevent removal from entityMap
	}

}
