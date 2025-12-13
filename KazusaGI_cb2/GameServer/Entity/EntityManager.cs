using System.Collections.Generic;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer;

public class EntityManager
{
	private readonly Session _session;
	private readonly Dictionary<uint, Entity> _entities = new();
	// Tracks recently removed entities and their removal time (for hit suppression)
	private readonly Dictionary<uint, DateTime> _recentlyRemovedEntities = new();
	private readonly TimeSpan _removalExpiry = TimeSpan.FromSeconds(2);

	public EntityManager(Session session)
	{
		_session = session;
	}

	public IReadOnlyDictionary<uint, Entity> Entities => _entities;

	public void Add(Entity entity, VisionType appearType = VisionType.VisionMeet)
	{
		_entities[entity._EntityId] = entity;
		// Remove from recently removed if re-added
		_recentlyRemovedEntities.Remove(entity._EntityId);

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
		// Track removal time
		_recentlyRemovedEntities[entityId] = DateTime.UtcNow;

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
	// Returns true if entity was recently removed (within expiry window)
	public bool WasRecentlyRemoved(uint entityId)
	{
		if (_recentlyRemovedEntities.TryGetValue(entityId, out var removedAt))
		{
			if ((DateTime.UtcNow - removedAt) < _removalExpiry)
				return true;
			// Expired, clean up
			_recentlyRemovedEntities.Remove(entityId);
		}
		return false;
	}
}
