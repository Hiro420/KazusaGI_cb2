using KazusaGI_cb2.GameServer.Ability;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using System.Numerics;

namespace KazusaGI_cb2.GameServer;

/// <summary>Per-scene runtime representation of a weapon/equipment gadget.</summary>
public class WeaponEntity : Entity
{
	public uint _gadgetId;
	public uint? WeaponItemId { get; }
	public PlayerWeapon? PersistentWeapon { get; }
	public WeaponExcelConfig? weaponExcel;

	/// <summary>
	/// Generic weapon gadget constructor used by monster weapon gadgets.
	/// </summary>
	public WeaponEntity(Session session, uint gadgetId, Vector3? position = null, Vector3? rotation = null)
		: this(session, gadgetId, null, position, rotation)
	{
	}

	/// <summary>
	/// Runtime entity for a persistent PACK weapon. The persistent object keeps
	/// the GUID/item state; this object receives a fresh Scene entity id.
	/// </summary>
	public WeaponEntity(Session session, PlayerWeapon weapon, Vector3? position = null, Vector3? rotation = null)
		: this(session, weapon.GadgetId, weapon.WeaponId, position, rotation)
	{
		PersistentWeapon = weapon;
	}

	private WeaponEntity(Session session, uint gadgetId, uint? weaponId, Vector3? position, Vector3? rotation)
		: base(session, position, rotation, ProtEntityType.ProtEntityWeapon)
	{
		_gadgetId = gadgetId;
		WeaponItemId = weaponId;
		if (weaponId.HasValue)
			MainApp.resourceManager.WeaponExcel.TryGetValue(weaponId.Value, out weaponExcel);

		abilityManager = new WeaponAbilityManager(this);
		abilityManager.Initialize();
	}

	protected override void BuildKindSpecific(SceneEntityInfo info)
	{
		// SceneWeaponInfo carries the item/gadget metadata. The bare runtime
		// entity only contributes entity identity and ability state.
	}

	public Dictionary<uint, uint> GetAffixMap()
	{
		if (PersistentWeapon != null)
			return new Dictionary<uint, uint>(PersistentWeapon.AffixMap);

		var affixMap = new Dictionary<uint, uint>();
		if (weaponExcel == null)
			return affixMap;

		// Monster/generic weapon gadgets have no PACK Weapon object, so seed
		// the config's initial affixes at level 0 exactly as Weapon::init does.
		foreach (uint affixId in weaponExcel.skillAffix ?? Enumerable.Empty<uint>())
			if (affixId != 0) affixMap.TryAdd(affixId, 0);
		return affixMap;
	}

}
