using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using System.Numerics;

namespace KazusaGI_cb2.GameServer;

public class SceneEntity : Entity
{
	// EntityMgr::createSceneEntity in CB2 assigns this exact id. Scene::findEntity
	// special-cases it because its high byte is not PROT_ENTITY_SCENE.
	public const uint FixedEntityId = 0x13800001u;

	public SceneAbilityManager AbilityManager => (SceneAbilityManager)abilityManager!;

	public SceneEntity(Session session, Vector3? position = null, Vector3? rotation = null)
		: base(session, position, rotation, ProtEntityType.ProtEntityScene, FixedEntityId)
	{
		abilityManager = new SceneAbilityManager(this);
		abilityManager.Initialize();
	}

	protected override void BuildKindSpecific(SceneEntityInfo info)
	{
		// Attach weather info via SceneGadgetInfo to mirror hk4e scene entity.
		var weather = new WeatherInfo
		{
			WeatherAreaId = 0
		};

		info.Gadget = new SceneGadgetInfo
		{
			Weather = weather
		};
	}

	// Override ForceKill to prevent SceneEntity from being removed from entityMap
	public override void ForceKill()
	{
		// Do nothing to prevent removal from entityMap
	}
}
