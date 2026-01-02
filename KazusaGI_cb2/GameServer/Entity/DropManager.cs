using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.ServerExcel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;

namespace KazusaGI_cb2.GameServer;

internal class DropManager
{
	private static readonly Random Rng = new(); // maybe will need later
	private static ResourceManager _resourceManager => MainApp.resourceManager;

	public static void DropGadgetLoot(Session session, GadgetEntity gadget)
	{
		if (gadget._gadgetLua == null || string.IsNullOrEmpty(gadget._gadgetLua.drop_tag))
		{
			// a little extra safety check
			return;
		}
		string dropTag = gadget._gadgetLua.drop_tag;

		// Find all ChestDropRows matching the drop tag
		var dropTree = _resourceManager.ServerChestDropRows
			.Where(dt => dt.DropTag == dropTag)
			.Where(d => d.MinLevel <= session.player.Level)
			.OrderBy(d => d.MinLevel)
			.LastOrDefault();

		if (dropTree == null)
		{
			//File.AppendAllLines("missing_droptag.log", [$"Missing drop tag: {dropTag}"]);
			session.c.LogWarning($"No drop tree found for drop tag: {dropTag}");
			return;
		}

		// Collect final material IDs using hk4e-like recursive drop processing
		List<uint> materialIds = new();
		HashSet<int> recursionGuard = new();
		CollectDrops(dropTree.DropId, dropTree.DropCount, session.player.Level, materialIds, recursionGuard, session, gadget);

		// Finally, spawn the actual drop items
		// first we group by material ID and count occurrences
		var materialIdCounts = materialIds
			.GroupBy(id => id)
			.ToDictionary(g => g.Key, g => (uint)g.Count());
		foreach (var kvp in materialIdCounts)
		{
			uint dropId = kvp.Key;
			uint count = kvp.Value;

			ItemType itemType;
			uint gadgetId;

			// First try material table
			if (_resourceManager.MaterialExcel.TryGetValue(dropId, out MaterialExcelConfig? materialExcelConfig) && materialExcelConfig != null)
			{
				itemType = materialExcelConfig.itemType;
				gadgetId = materialExcelConfig.gadgetId;
			}
			// Then try reliquary table (artifact items)
			else if (_resourceManager.ReliquaryExcel.TryGetValue(dropId, out ReliquaryExcelConfig? reliquaryExcel) && reliquaryExcel != null)
			{
				itemType = ItemType.ITEM_RELIQUARY;
				gadgetId = reliquaryExcel.gadgetId;
			}
			else
			{
				session.c.LogWarning($"No item config found for drop ID: {dropId}");
				continue;
			}

			// Virtual / direct-inventory items are not spawned as world gadgets here
			if (itemType == ItemType.ITEM_VIRTUAL)
			{
				// TODO: handle inventory-only rewards
				continue;
			}

			if (gadgetId == 0)
			{
				session.c.LogWarning($"Drop item {dropId} has no gadgetId, skipping world drop.");
				continue;
			}

			DropItemEntity gadgetEntity = new DropItemEntity(
				session,
				gadgetId,
				dropId,
				itemType,
				count
			);
			session.player.Scene.EntityManager.Add(gadgetEntity);

			SceneEntityAppearNotify pkt = new SceneEntityAppearNotify()
			{
				AppearType = Protocol.VisionType.VisionReborn,
			};
			pkt.EntityLists.Add(gadgetEntity.ToSceneEntityInfo());

			session.SendPacket(pkt);

			session.c.LogInfo($"Dropped item {dropId} x{count} for gadget {gadget._gadgetId}");

			// reliquary main/append stats are generated later in DropItemEntity
		}
	}

	private readonly struct DropChild
	{
		public DropChild(int id, int weight, string? range)
		{
			Id = id;
			Weight = weight;
			Range = range;
		}

		public int Id { get; }
		public int Weight { get; }
		public string? Range { get; }
	}

	private static List<DropChild> ExtractChildren(object config, int maxSlots)
	{
		var children = new List<DropChild>();
		var type = config.GetType();
		for (int i = 1; i <= maxSlots; i++)
		{
			var idProp = type.GetProperty($"SubDrop{i}Id");
			var weightProp = type.GetProperty($"SubDrop{i}Weight");
			var rangeProp = type.GetProperty($"SubDrop{i}CountRange");

			if (idProp == null || weightProp == null)
				continue;

			var idVal = idProp.GetValue(config) as int?;
			var weightVal = weightProp.GetValue(config) as int?;
			var rangeVal = rangeProp?.GetValue(config) as string;

			if (idVal is > 0 && weightVal is > 0)
				children.Add(new DropChild(idVal.Value, weightVal.Value, rangeVal));
		}

		return children;
	}

	private static int GetCountFromRange(string? range)
	{
		if (string.IsNullOrWhiteSpace(range))
			return 1;

		string trimmed = range.Trim();
		char[] separators = new[] { '-', '~', ',', '|' };
		string[] parts = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length == 1)
		{
			if (int.TryParse(parts[0].Trim(), out int single) && single > 0)
				return single;
			return 1;
		}

		if (!int.TryParse(parts[0].Trim(), out int a) || !int.TryParse(parts[1].Trim(), out int b))
			return 1;

		int min = Math.Min(a, b);
		int max = Math.Max(a, b);

		if (max <= 0)
			return 1;
		if (min <= 0)
			min = 1;

		return Rng.Next(min, max + 1);
	}

	private static void CollectDrops(int nodeId, int times, int playerLevel, List<uint> materialIds, HashSet<int> recursionGuard, Session session, GadgetEntity gadget)
	{
		if (times <= 0)
			return;

		if (!recursionGuard.Add(nodeId))
		{
			session.c.LogWarning($"Detected recursive drop node reference for ID: {nodeId}, aborting to prevent infinite loop.");
			return;
		}

		try
		{
			DropTreeRow? tree = _resourceManager.ServerDropTreeRows.FirstOrDefault(d => d.Id == nodeId);
			DropLeafRow? leaf = _resourceManager.ServerDropLeafRows.FirstOrDefault(d => d.Id == nodeId);

			// Base case: no tree/leaf config, treat as final drop ID (item/reliquary/etc.)
			if (tree == null && leaf == null)
			{
				for (int i = 0; i < times; i++)
				{
					materialIds.Add((uint)nodeId);
				}
				return;
			}

			int? minLevel = tree?.MinLevel ?? leaf?.MinLevel;
			int? maxLevel = tree?.MaxLevel ?? leaf?.MaxLevel;
			if (minLevel.HasValue && playerLevel < minLevel.Value)
				return;
			if (maxLevel.HasValue && playerLevel > maxLevel.Value)
				return;

			object config = (object?)tree ?? leaf!;
			int maxSlots = tree != null ? 30 : 25;
			List<DropChild> children = ExtractChildren(config, maxSlots);
			if (children.Count == 0)
				return;

			int randomType = tree?.RandomType ?? leaf?.RandomType ?? 0;

			for (int t = 0; t < times; t++)
			{
				if (randomType == 1)
				{
					// Independent rolls per child
					foreach (var child in children)
					{
						if (Utils.WeightedRandom.RollByWeight(child.Weight))
						{
							int count = GetCountFromRange(child.Range);
							if (count > 0)
								CollectDrops(child.Id, count, playerLevel, materialIds, recursionGuard, session, gadget);
						}
					}
				}
				else
				{
					// By-weight: choose exactly one child
					uint picked = Utils.WeightedRandom.PickOne(children.ConvertAll(c => ((uint)c.Id, c.Weight)));
					if (picked == 0)
					{
						session.c.LogWarning($"[DropManager] No child picked for drop node {nodeId}");
						continue;
					}

					DropChild childEntry = children.FirstOrDefault(c => c.Id == (int)picked);
					if (childEntry.Id == 0)
					{
						session.c.LogWarning($"[DropManager] Picked child {picked} not found in children list for node {nodeId}");
						continue;
					}

					int count = GetCountFromRange(childEntry.Range);
					if (count > 0)
						CollectDrops(childEntry.Id, count, playerLevel, materialIds, recursionGuard, session, gadget);
				}
			}
		}
		finally
		{
			recursionGuard.Remove(nodeId);
		}
	}

	public static void DropLoot(Session session, uint dropId, Entity entity)
	{
		var resourceManager = MainApp.resourceManager;
		var dropConfig = resourceManager.ServerDropTreeRows.Find(row => row.Id == dropId);
		if (dropConfig == null)
		{
			session.c.LogWarning($"No tree drop config found for drop ID {dropId} on entity ID {entity._EntityId}");
			return;
		}

		// Build child list from DropTreeRow (SubDrop1..30) via reflection
		var children = new List<(uint Id, int Weight, string? Range)>();
		var type = dropConfig.GetType();
		for (int i = 1; i <= 30; i++)
		{
			var idProp = type.GetProperty($"SubDrop{i}Id");
			var weightProp = type.GetProperty($"SubDrop{i}Weight");
			var rangeProp = type.GetProperty($"SubDrop{i}CountRange");

			if (idProp == null || weightProp == null)
				continue;

			var idVal = idProp.GetValue(dropConfig) as int?;
			var weightVal = weightProp.GetValue(dropConfig) as int?;
			var rangeVal = rangeProp?.GetValue(dropConfig) as string;

			if (idVal is > 0 && weightVal is > 0)
				children.Add(((uint)idVal.Value, weightVal.Value, rangeVal));
		}

		if (children.Count == 0)
			return;

		int randomType = dropConfig.RandomType ?? 0;

		List<uint> materialIds2Drop = new();

		// RandomType 0: choose exactly one child by weight (hk4e "by weight").
		// RandomType 1: independent rolls per child (hk4e DROP_RANDOM_INDEPENDENT).
		if (randomType == 0)
		{
			uint pickedId = Utils.WeightedRandom.PickOne(children.ConvertAll(c => (c.Id, c.Weight)));
			if (pickedId == 0)
			{
				session.c.LogWarning($"[Internal Error] Entity {entity._EntityId} drop {dropId}: no child drop picked by weight");
				return;
			}
			materialIds2Drop.Add(pickedId);
		}
		else if (randomType == 1)
		{
			foreach (var child in children)
			{
				if (Utils.WeightedRandom.RollByWeight(child.Weight))
				{
					materialIds2Drop.Add(child.Id);
				}
			}
		}

		// Finally, spawn the picked material drops in the scene.
		foreach (var matId in materialIds2Drop)
		{
			var materialConfig = resourceManager.MaterialExcel.Values.FirstOrDefault(m => m.id == matId);
			if (materialConfig == null)
			{
				session.c.LogWarning($"No material config found for material ID {matId} dropped by entity ID {entity._EntityId}");
				continue;
			}
			// Determine drop count
			int dropCount = 1;
			var child = children.Find(c => c.Id == matId);
			if (child.Range != null)
			{
				var parts = child.Range.Split('-');
				if (parts.Length == 2 &&
					int.TryParse(parts[0], out int min) &&
					int.TryParse(parts[1], out int max))
				{
					dropCount = new Random().Next(min, max + 1);
				}
			}
			List<DropItemEntity> droppedGadgets = new();
			var gadgetEntity = new DropItemEntity(
				session: session,
				gadgetId: materialConfig.gadgetId,
				trifleItemId: matId,
				itemTypem: materialConfig.itemType,
				count: (uint)dropCount
			);
			session.player!.Scene.EntityManager.Add(gadgetEntity);
			droppedGadgets.Add(gadgetEntity);
			session.c.LogInfo($"Entity {entity._EntityId} dropped {dropCount}x material ID {matId} at position {entity.Position}");
			SceneEntityAppearNotify appearNotify = new SceneEntityAppearNotify
			{
				AppearType = Protocol.VisionType.VisionMeet,
			};
			foreach (var dg in droppedGadgets)
			{
				appearNotify.EntityLists.Add(dg.ToSceneEntityInfo());
			}
			session.SendPacket(appearNotify);
		}
	}
}
