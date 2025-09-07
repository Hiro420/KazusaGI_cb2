using System.Numerics;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;

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
		}

		protected override void BuildKindSpecific(SceneEntityInfo info)
		{
			// Currently no need
		}
	}
}
