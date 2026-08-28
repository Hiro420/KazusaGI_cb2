using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

/// <summary>
/// Persistent PACK weapon. This object deliberately owns no Scene entity:
/// hk4e separates an Item/Equip GUID from the per-scene Weapon entity id.
/// Scene.GetOrCreateWeaponEntity is the only place that creates that runtime id.
/// </summary>
public class PlayerWeapon
{
	private Session Session { get; }
	private WeaponExcelConfig weaponExcel;
	private static ResourceManager resourceManager = MainApp.resourceManager;

	public ulong Guid { get; set; }
	// Runtime WeaponGadget entity_id_. Scene::delAvatarAndWeaponEntity clears it.
	public uint EntityId { get; internal set; }
	public uint WeaponId { get; set; }
	public uint Level { get; set; }
	public uint Exp { get; set; }
	public uint PromoteLevel { get; set; }
	public uint GadgetId { get; set; }

	// Weapon::affix_map_ lives on the persistent Weapon item, not on the
	// transient WeaponGadget. Keys are WeaponExcelConfig::skill_affix ids; a
	// freshly-created weapon stores level 0 for each initial affix.
	public Dictionary<uint, uint> AffixMap { get; } = new();

	/// <summary>Avatar GUID which owns this equip, or null when unequipped.</summary>
	public ulong? EquipGuid { get; set; }

	public PlayerWeapon(Session session, uint weaponId, ulong? overrideGuid = null)
	{
		Session = session;
		weaponExcel = resourceManager.WeaponExcel[weaponId];
		Guid = overrideGuid ?? MainApp.GuidMgr.GenGuid(GuidMgr.GuidType.Item);
		WeaponId = weaponId;
		// Weapon::init on a newly-created item starts from born state. Progress
		// is applied later by level/promote operations or restored from bin.
		Level = 1;
		Exp = 0;
		PromoteLevel = 0;
		GadgetId = weaponExcel.gadgetId;
		foreach (uint affixId in weaponExcel.skillAffix ?? Enumerable.Empty<uint>())
			if (affixId != 0) AffixMap.TryAdd(affixId, 0);

		if (!session.player!.weaponDict.TryAdd(Guid, this))
			throw new InvalidOperationException($"duplicate PACK weapon guid {Guid}");
	}

	/// <summary>
	/// EquipComp::wearEquip(EQUIP_WEAPON). If the incoming weapon is already
	/// worn by another avatar, hk4e swaps the target avatar's old weapon back
	/// to that previous owner. A move which would leave that owner weaponless
	/// is rejected.
	/// </summary>
	public int EquipOnAvatar(PlayerAvatar avatar, bool broadcastPacket)
	{
		var player = Session.player!;
		PlayerAvatar? previousOwner = EquipGuid is ulong previousAvatarGuid
			? player.avatarDict.GetValueOrDefault(previousAvatarGuid)
			: null;
		PlayerWeapon? oldTargetWeapon = avatar.EquipGuid != 0
			? player.weaponDict.GetValueOrDefault(avatar.EquipGuid)
			: null;

		if (avatar.EquipGuid == Guid && EquipGuid == avatar.Guid)
			return 0;

		// Exact EquipComp guard: moving another avatar's weapon onto an avatar
		// which has no weapon gives the previous owner nothing to swap back.
		if (previousOwner != null && previousOwner.Guid != avatar.Guid && oldTargetWeapon == null)
			return -1;

		if (previousOwner != null && previousOwner.Guid != avatar.Guid)
			previousOwner.EquipGuid = 0;

		if (oldTargetWeapon != null && oldTargetWeapon.Guid != Guid)
		{
			oldTargetWeapon.EquipGuid = null;
			if (previousOwner != null && previousOwner.Guid != avatar.Guid)
			{
				previousOwner.EquipGuid = oldTargetWeapon.Guid;
				oldTargetWeapon.EquipGuid = previousOwner.Guid;
			}
		}

		avatar.EquipGuid = Guid;
		EquipGuid = avatar.Guid;

		// Changing persistent equip does not manufacture a scene entity. If the
		// avatar is currently instantiated, rebuild only its weapon-gadget
		// binding through Scene, matching EquipComp + Scene ownership.
		if (player.Scene != null && avatar.EntityId != 0)
			player.Scene.OnAvatarWeaponChanged(avatar);

		if (previousOwner != null && previousOwner.Guid != avatar.Guid &&
			player.Scene != null && previousOwner.EntityId != 0)
			player.Scene.OnAvatarWeaponChanged(previousOwner);

		if (broadcastPacket)
		{
			Session.SendPacket(new AvatarEquipChangeNotify
			{
				AvatarGuid = avatar.Guid,
				Weapon = ToSceneWeaponInfo(Session),
				EquipGuid = Guid,
				EquipType = (uint)EquipType.EQUIP_WEAPON,
				ItemId = WeaponId
			});
		}
		return 0;
	}

	// returns (maxPromoteLevel, maxWeaponLevel)
	public (uint, uint) GetMaxWeaponPromote(WeaponExcelConfig excel)
	{
		uint promoteId = (uint)excel.weaponPromoteId;
		Dictionary<uint, WeaponPromoteExcelConfig>? relevantPromotes =
			resourceManager.WeaponPromoteExcel.TryGetValue(promoteId, out var configDict) ? configDict : null;
		if (relevantPromotes == null || relevantPromotes.Count == 0)
			return (1, 1);

		WeaponPromoteExcelConfig highestPromote = relevantPromotes.Values
			.OrderByDescending(wp => wp.promoteLevel)
			.First();
		return (highestPromote.promoteLevel, highestPromote.unlockMaxLevel);
	}

	public SceneWeaponInfo ToSceneWeaponInfo(Session session)
	{
		WeaponEntity weaponEntity = session.player!.Scene.GetOrCreateWeaponEntity(this);
		var info = new SceneWeaponInfo
		{
			EntityId = weaponEntity._EntityId,
			GadgetId = GadgetId,
			ItemId = WeaponId,
			Guid = Guid,
			Level = Level,
			PromoteLevel = PromoteLevel,
			AbilityInfo = weaponEntity.BuildAbilityInfo()
		};

		foreach (var (affixId, affixLevel) in weaponEntity.GetAffixMap())
		{
			if (affixId != 0)
				info.AffixMaps[affixId] = affixLevel;
		}
		return info;
	}
}
