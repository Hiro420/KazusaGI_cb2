using System.Collections.Generic;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer;

public class EntityManager
{
	private readonly Session _session;
	private readonly Dictionary<uint, Entity> _entities = new();

	public EntityManager(Session session)
	{
		_session = session;
	}

	public IReadOnlyDictionary<uint, Entity> Entities => _entities;

	public void Add(Entity entity, VisionType appearType = VisionType.VisionMeet)
	{
		_entities[entity._EntityId] = entity;

		var notify = new SceneEntityAppearNotify
		{
			AppearType = appearType
		};
		notify.EntityLists.Add(entity.ToSceneEntityInfo());
		_session.SendPacket(notify);
	}

	public bool Remove(uint entityId, VisionType disappearType = VisionType.VisionDie)
	{
		if (!_entities.Remove(entityId))
			return false;

		_session.SendPacket(new LifeStateChangeNotify
		{
			EntityId = entityId,
			LifeState = 2
		});
		_session.SendPacket(new SceneEntityDisappearNotify
		{
			DisappearType = disappearType,
			EntityLists = { entityId }
		});
		return true;
	}

	public bool TryGet(uint entityId, out Entity entity) => _entities.TryGetValue(entityId, out entity!);
}
