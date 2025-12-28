using System;
using System.Collections.Generic;
using System.Linq;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer;

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

	/// <summary>
	/// Registers a single entity in the live map without sending any packets.
	/// Callers are responsible for sending SceneEntityAppearNotify if needed.
	/// </summary>
	public void Add(Entity entity)
	{
		if (entity == null)
			throw new ArgumentNullException(nameof(entity));

		_entities[entity._EntityId] = entity;
		_recentlyRemovedEntities.Remove(entity._EntityId);
	}

	/// <summary>
	/// Registers a batch of entities without emitting any network traffic.
	/// </summary>
	public void AddRange(IEnumerable<Entity> entities)
	{
		if (entities == null)
			throw new ArgumentNullException(nameof(entities));

		foreach (var entity in entities)
		{
			if (entity == null)
				continue;
			Add(entity);
		}
	}

	/// <summary>
	/// Removes an entity from the live map and optionally sends standard
	/// LifeStateChangeNotify + SceneEntityDisappearNotify for that id.
	///
	/// This mirrors hk4e behavior for one-off despawns (gather, destroy,
	/// scene transitions). For bulk despawns that already build their own
	/// SceneEntityDisappearNotify, pass notifyClients = false.
	/// </summary>
	public bool Remove(uint entityId, VisionType disappearType = VisionType.VisionDie, bool notifyClients = true)
	{
		if (!_entities.Remove(entityId))
			return false;

		_recentlyRemovedEntities[entityId] = DateTime.UtcNow;

		if (notifyClients)
		{
			_session.SendPacket(new LifeStateChangeNotify
			{
				EntityId = entityId,
				LifeState = 2
			});

			var disappear = new SceneEntityDisappearNotify
			{
				DisappearType = disappearType
			};
			disappear.EntityLists.Add(entityId);
			_session.SendPacket(disappear);
		}

		return true;
	}

	/// <summary>
	/// Bulk despawn helper that removes many entities at once and emits a
	/// single SceneEntityDisappearNotify mirroring hk4e's batched behavior.
	/// </summary>
	public void DespawnMany(IEnumerable<uint> entityIds, VisionType disappearType)
	{
		if (entityIds == null)
			throw new ArgumentNullException(nameof(entityIds));

		var removedIds = new List<uint>();

		foreach (var id in entityIds)
		{
			if (!_entities.Remove(id))
				continue;

			_recentlyRemovedEntities[id] = DateTime.UtcNow;
			removedIds.Add(id);
		}

		if (removedIds.Count == 0)
			return;

		// Send life-state changes first, then a single batched disappear.
		foreach (var id in removedIds)
		{
			_session.SendPacket(new LifeStateChangeNotify
			{
				EntityId = id,
				LifeState = 2
			});
		}

		var disappear = new SceneEntityDisappearNotify
		{
			DisappearType = disappearType
		};
		disappear.EntityLists.AddRange(removedIds);
		_session.SendPacket(disappear);
	}

	public bool TryGet(uint entityId, out Entity entity) => _entities.TryGetValue(entityId, out entity!);

	/// <summary>
	/// Returns true if an entity id was removed recently enough that late
	/// hits or ability callbacks should be ignored.
	/// </summary>
	public bool WasRecentlyRemoved(uint entityId)
	{
		if (_recentlyRemovedEntities.TryGetValue(entityId, out var removedAt))
		{
			if ((DateTime.UtcNow - removedAt) < _removalExpiry)
				return true;

			// Expired, clean up bookkeeping.
			_recentlyRemovedEntities.Remove(entityId);
		}
		return false;
	}
}
