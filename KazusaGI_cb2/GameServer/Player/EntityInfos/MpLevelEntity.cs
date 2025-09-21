using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;

namespace KazusaGI_cb2.GameServer
{
	public class MpLevelEntity : Entity
	{

		public MpLevelEntity(Session session, Vector3? position = null, Vector3? rotation = null)
			: base(session, position, rotation, ProtEntityType.ProtEntityMpLevel)
		{
		}

		protected override void BuildKindSpecific(SceneEntityInfo info)
		{
			// Currently no need
		}

		// Override ForceKill to prevent MpLevelEntity from being removed from entityMap
		public override void ForceKill()
		{
			// MpLevelEntity should never die or be removed from entityMap
			// This is a persistent entity that represents the multiplayer level
			// Do nothing to prevent removal from entityMap
		}
	}
}
