using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer;

/// <summary>
/// Live entity registry for one Scene instance. Registration/removal is kept
/// separate from life-state transitions: leaving vision or changing scenes is
/// not equivalent to dying in hk4e.
/// </summary>
public class EntityManager
{
	private readonly Session _session;
	private readonly Dictionary<uint, Entity> _entities = new();
	private readonly Dictionary<uint, DateTime> _recentlyRemovedEntities = new();
	private readonly TimeSpan _removalExpiry = TimeSpan.FromSeconds(2);

	public EntityManager(Session session)
	{
		_session = session;
	}

	public IReadOnlyDictionary<uint, Entity> Entities => _entities;

	private static void ClearRuntimeIdentity(Entity entity)
	{
		if (entity is AvatarEntity avatar)
		{
			avatar.DbInfo.EntityId = 0;
			avatar.DbInfo.LastMoveSceneTimeMs = 0;
			avatar.DbInfo.LastMoveReliableSeq = 0;
			avatar.DbInfo.LastMoveParams.Clear();
		}
		else if (entity is WeaponEntity { PersistentWeapon: not null } weapon)
		{
			weapon.PersistentWeapon.EntityId = 0;
		}
	}

	public void Add(Entity entity)
	{
		ArgumentNullException.ThrowIfNull(entity);

		if (_entities.TryGetValue(entity._EntityId, out var existing) && !ReferenceEquals(existing, entity))
			throw new InvalidOperationException($"duplicate runtime entity id 0x{entity._EntityId:X8}");

		_entities[entity._EntityId] = entity;
		_recentlyRemovedEntities.Remove(entity._EntityId);
	}

	public void AddRange(IEnumerable<Entity> entities)
	{
		ArgumentNullException.ThrowIfNull(entities);
		foreach (var entity in entities)
			if (entity != null)
				Add(entity);
	}

	/// <summary>
	/// Removes an entity from the live map. By default this emits only a
	/// disappear notify. Death code must emit its own LifeStateChangeNotify,
	/// because VisionMiss/VisionReplace/GatherEscape are not deaths.
	/// </summary>
	public bool Remove(
		uint entityId,
		VisionType disappearType = VisionType.VisionDie,
		bool notifyClients = true,
		bool notifyLifeState = false)
	{
		if (!_entities.Remove(entityId, out var removed))
			return false;
		ClearRuntimeIdentity(removed);

		_recentlyRemovedEntities[entityId] = DateTime.UtcNow;

		if (!notifyClients)
			return true;

		if (notifyLifeState)
		{
			_session.SendPacket(new LifeStateChangeNotify
			{
				EntityId = entityId,
				LifeState = 2
			});
		}

		var disappear = new SceneEntityDisappearNotify
		{
			DisappearType = disappearType
		};
		disappear.EntityLists.Add(entityId);
		_session.SendPacket(disappear);
		return true;
	}

	/// <summary>Remove registry state only, with no network semantics.</summary>
	public bool Unregister(uint entityId)
	{
		if (!_entities.Remove(entityId, out var removed))
			return false;
		ClearRuntimeIdentity(removed);
		_recentlyRemovedEntities[entityId] = DateTime.UtcNow;
		return true;
	}

	public void DespawnMany(IEnumerable<uint> entityIds, VisionType disappearType, bool notifyLifeState = false)
	{
		ArgumentNullException.ThrowIfNull(entityIds);
		var removedIds = new List<uint>();

		foreach (var id in entityIds.Distinct())
		{
			if (!_entities.Remove(id, out var removed))
				continue;
			ClearRuntimeIdentity(removed);
			_recentlyRemovedEntities[id] = DateTime.UtcNow;
			removedIds.Add(id);
		}

		if (removedIds.Count == 0)
			return;

		if (notifyLifeState)
		{
			foreach (var id in removedIds)
				_session.SendPacket(new LifeStateChangeNotify { EntityId = id, LifeState = 2 });
		}

		var disappear = new SceneEntityDisappearNotify { DisappearType = disappearType };
		disappear.EntityLists.AddRange(removedIds);
		_session.SendPacket(disappear);
	}

	public bool TryGet(uint entityId, out Entity entity) => _entities.TryGetValue(entityId, out entity!);

	public bool WasRecentlyRemoved(uint entityId)
	{
		if (!_recentlyRemovedEntities.TryGetValue(entityId, out var removedAt))
			return false;

		if ((DateTime.UtcNow - removedAt) < _removalExpiry)
			return true;

		_recentlyRemovedEntities.Remove(entityId);
		return false;
	}
}
