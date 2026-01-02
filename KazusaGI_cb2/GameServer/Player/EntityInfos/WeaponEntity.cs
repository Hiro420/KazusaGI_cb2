using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System.Numerics;
using System.Resources;

namespace KazusaGI_cb2.GameServer
{
	public class WeaponEntity : Entity
	{
		public uint _gadgetId;
		public WeaponExcelConfig? weaponExcel;

		public WeaponEntity(Session session, uint gadgetId, uint? weaponId = null, Vector3? position = null, Vector3? rotation = null)
			: base(session, position, rotation, ProtEntityType.ProtEntityWeapon)
		{
			_gadgetId = gadgetId;
			if (weaponId != null)
				weaponExcel = MainApp.resourceManager.WeaponExcel[gadgetId];
			abilityManager = new WeaponAbilityManager(this);
			abilityManager.Initialize();
		}

		protected override void BuildKindSpecific(SceneEntityInfo info)
		{
			// Currently no need
		}

		public Dictionary<uint, uint> GetAffixMap()
		{
			var resourceManager = MainApp.resourceManager;
			Dictionary<uint, uint> affixMap = new();
			foreach (uint affixId in weaponExcel?.skillAffix ?? Enumerable.Empty<uint>())
			{
				if (affixId == 0) continue;
				List<EquipAffixExcelConfig>? affixConfigs = resourceManager.EquipAffixExcel.Values.Where(e => e.AffixId == affixId).ToList();
				foreach (var affixConfig in affixConfigs)
				{
					// todo: actually handle it properly later
					affixMap[affixConfig.Id] = affixConfig.Level;
				}
			}
			return affixMap;
		}
	}
}
