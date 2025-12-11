using System.Numerics;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;

namespace KazusaGI_cb2.GameServer
{
	public class TeamEntity : Entity
	{
		public TeamAbilityManager AbilityManager { get; private set; }

		public TeamEntity(Session session, Vector3? position = null, Vector3? rotation = null)
			: base(session, position, rotation, ProtEntityType.ProtEntityTeam)
		{
			AbilityManager = new TeamAbilityManager(this);
			AbilityManager.Initialize();
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
}
