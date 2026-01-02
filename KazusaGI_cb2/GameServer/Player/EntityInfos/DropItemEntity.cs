using System.Collections.Generic;
using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Resource.Json.Ability.Temp;
using System.Linq;
using KazusaGI_cb2.Resource.Json.Avatar;
using Newtonsoft.Json;
using System;
using System.IO;
using KazusaGI_cb2.Resource.Json.Ability.Temp.BornTypes;
using KazusaGI_cb2.Resource.ServerExcel;
using KazusaGI_cb2.Utils;

namespace KazusaGI_cb2.GameServer
{
	public class DropItemEntity : Entity
	{
		public uint _trifleItemId;
		public uint _trifleItemCount;
		public uint _gadgetId;
		public GadgetExcelConfig gadgetExcel;
		public ConfigGadget? configGadget;
		public GadgetRow serverExcelConfig;
		public bool isEnableInteract;
		public ItemType itemType;

		public DropItemEntity(Session session, uint gadgetId, uint trifleItemId, ItemType itemTypem, uint? count)
		: base(session, null, null, ProtEntityType.ProtEntityGadget, null)
		{
			_trifleItemId = trifleItemId;
			_trifleItemCount = count ?? 1;
			itemType = itemTypem;
			_gadgetId = gadgetId;
			gadgetExcel = MainApp.resourceManager.GadgetExcel[gadgetId];
			serverExcelConfig = MainApp.resourceManager.ServerGadgetRows.First(i => i.Id == gadgetId)!;
			isEnableInteract = true;

			session.player.Scene.EntityManager.Add(this);
		}

		protected override Dictionary<uint, float> GetFightProps()
		{
			var ret = new Dictionary<uint, float>
			{
				[(uint)FightPropType.FIGHT_PROP_BASE_HP] = 1f,
				[(uint)FightPropType.FIGHT_PROP_CUR_HP] = 1f,
				[(uint)FightPropType.FIGHT_PROP_MAX_HP] = 1f
			};
			return ret;
		}
		protected override void BuildKindSpecific(SceneEntityInfo ret)
		{
			ret.Name = gadgetExcel.jsonName;

			var bornType = GadgetBornType.GadgetBornGadget;
			var type = gadgetExcel.type;
			// In hk4e, when born_type_ is 0:
			//   if (type <= 24) born_type = (type >= 23) ? 1 : 0; // IN_AIR for AirflowField/SpeedupField
			//   else if (type == 34) born_type = 6;               // GROUND for EnvAnimal
			if (type <= GadgetType_Excel.SpeedupField)
			{
				if (type >= GadgetType_Excel.AirflowField)
					bornType = GadgetBornType.GadgetBornInAir;
			}
			else if (type == GadgetType_Excel.EnvAnimal)
			{
				bornType = GadgetBornType.GadgetBornGround;
			}

			var info = new SceneGadgetInfo
			{
				AuthorityPeerId = session.player!.PeerId,
				IsEnableInteract = isEnableInteract,
				GadgetId = _gadgetId,
				BornType = bornType,
			};

			ret.Gadget = info;

			Item trifleItem = new Item
			{
				ItemId = _trifleItemId,
				Guid = 0, // todo: generate unique guid for the item
			};

			switch (itemType)
			{
				case ItemType.ITEM_RELIQUARY:
					trifleItem.Equip = new Equip
					{
						Reliquary = GenerateRandomReliquary(_trifleItemId)
					};
					break;
				case ItemType.ITEM_WEAPON:
					trifleItem.Equip = new Equip
					{
						Weapon = new Weapon
						{
							Level = 1,
							Exp = 0,
							PromoteLevel = 0
						}
					};
					break;
				case ItemType.ITEM_MATERIAL:
					trifleItem.Material = new Material
					{
						Count = _trifleItemCount,
					};
					break;
			}

			info.TrifleItem = trifleItem;
		}

		public Reliquary GenerateRandomReliquary(uint reliquaryId)
		{
			var rm = MainApp.resourceManager;
			if (!rm.ReliquaryExcel.TryGetValue(reliquaryId, out ReliquaryExcelConfig? reliquaryExcel) || reliquaryExcel == null)
			{
				session.c.LogError($"Reliquary Excel not found for ID: {reliquaryId}");
				return new Reliquary(); // fallback; item should always have valid config
			}

			var reliquary = new Reliquary
			{
				Level = 1,
				Exp = 0,
				PromoteLevel = 0,
				MainPropId = GetRandomMainPropForReliquary(reliquaryExcel),
			};

			// Randomly select append properties (substats) using ReliquaryAffixExcel
			uint appendDepotId = reliquaryExcel.appendPropDepotId;
			var allAffixes = rm.ReliquaryAffixExcel.Values
				.Where(a => a.depotId == appendDepotId)
				.ToList();
			if (allAffixes.Count == 0)
			{
				session.c.LogWarning($"No ReliquaryAffixExcel rows for depotId {appendDepotId} (reliquary {reliquaryId})");
				return reliquary;
			}

			// Group by groupId so we don't pick the same substat group multiple times
			var groups = allAffixes
				.GroupBy(a => a.groupId)
				.ToList();

			int targetSubCount = (int)reliquaryExcel.appendPropNum;
			if (targetSubCount <= 0)
				return reliquary;
			if (targetSubCount > groups.Count)
				targetSubCount = groups.Count;

			var availableGroups = new List<IGrouping<uint, ReliquaryAffixExcelConfig>>(groups);
			while (reliquary.AppendPropIdLists.Count < targetSubCount && availableGroups.Count > 0)
			{
				// Pick a group by the sum of its member weights
				var groupWeighted = new List<(IGrouping<uint, ReliquaryAffixExcelConfig> Group, int Weight)>();
				foreach (var g in availableGroups)
				{
					long sum = 0;
					foreach (var affix in g)
						sum += affix.weight;
					if (sum > 0)
						groupWeighted.Add((g, (int)Math.Min(sum, int.MaxValue)));
				}
				if (groupWeighted.Count == 0)
					break;

				var pickedGroup = WeightedRandom.PickOne(groupWeighted.ConvertAll(e => (e.Group, e.Weight)));
				if (pickedGroup == null)
					break;

				// Within the group, pick a concrete affix row by weight
				var affixList = new List<ReliquaryAffixExcelConfig>(pickedGroup);
				var affixWeighted = new List<(ReliquaryAffixExcelConfig Affix, int Weight)>();
				foreach (var affix in affixList)
				{
					if (affix.weight > 0)
						affixWeighted.Add((affix, (int)Math.Min(affix.weight, int.MaxValue)));
				}
				if (affixWeighted.Count == 0)
				{
					availableGroups.Remove(pickedGroup);
					continue;
				}

				var pickedAffix = WeightedRandom.PickOne(affixWeighted.ConvertAll(e => (e.Affix, e.Weight)));
				if (pickedAffix == null)
				{
					availableGroups.Remove(pickedGroup);
					continue;
				}

				reliquary.AppendPropIdLists.Add(pickedAffix.id);
				availableGroups.RemoveAll(g => g.Key == pickedGroup.Key);
			}

			return reliquary;
		}

		public uint GetRandomMainPropForReliquary(ReliquaryExcelConfig reliquaryExcel)
		{
			uint mainDepotId = reliquaryExcel.mainPropDepotId;
			var candidates = MainApp.resourceManager.ReliquaryMainPropExcel.Values
				.Where(p => p.propDepotId == mainDepotId)
				.ToList();
			if (candidates.Count == 0)
			{
				session.c.LogWarning($"No ReliquaryMainPropExcel rows for depotId {mainDepotId} (reliquary {reliquaryExcel.id})");
				return 0;
			}

			var weighted = new List<(ReliquaryMainPropExcelConfig Prop, int Weight)>();
			foreach (var p in candidates)
			{
				if (p.weight > 0)
					weighted.Add((p, (int)Math.Min(p.weight, int.MaxValue)));
			}
			if (weighted.Count == 0)
			{
				// fallback to uniform choice
				var rnd = new Random();
				return candidates[rnd.Next(candidates.Count)].id;
			}

			var picked = WeightedRandom.PickOne(weighted.ConvertAll(e => (e.Prop, e.Weight)));
			return picked?.id ?? 0;
		}
	}
}
