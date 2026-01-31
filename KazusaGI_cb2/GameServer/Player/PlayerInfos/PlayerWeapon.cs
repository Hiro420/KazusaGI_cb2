using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

public class PlayerWeapon
{
    private Session Session { get; set; }
    private WeaponExcelConfig weaponExcel;
	private static ResourceManager resourceManager = MainApp.resourceManager;
    public ulong Guid { get; set; } // critical
    public uint WeaponId { get; set; } // critical
    public uint Level { get; set; }
    public uint Exp { get; set; }
    public uint PromoteLevel { get; set; }
    public uint GadgetId { get; set; }
    public uint WeaponEntityId { get; set; }
    public ulong? EquipGuid { get; set; }

    // add affixes later TODO

    public PlayerWeapon(Session session, uint WeaponId)
    {
        this.Session = session;
        WeaponEntity weaponEntity = new(session, WeaponId);
        weaponExcel = resourceManager.WeaponExcel[WeaponId];
        (uint maxPromoteLevel, uint maxWeaponLevel) = GetMaxWeaponPromote(weaponExcel);
		this.Guid = session.GetGuid();
        this.WeaponId = WeaponId;
        this.Level = maxWeaponLevel;
        this.Exp = 0;
        this.PromoteLevel = maxPromoteLevel;
        this.GadgetId = weaponExcel.gadgetId;
        this.WeaponEntityId = weaponEntity._EntityId;
        session.player!.weaponDict.Add(this.Guid, this);
        session.player.Scene.EntityManager.Add(weaponEntity);
    }

    public void EquipOnAvatar(PlayerAvatar avatar, bool broadcastPacket)
    {
        avatar.EquipGuid = this.Guid;
        this.EquipGuid = avatar.Guid;
        if (broadcastPacket)
        {
            AvatarEquipChangeNotify ntf = new AvatarEquipChangeNotify()
            {
                AvatarGuid = avatar.Guid,
                Weapon = this.ToSceneWeaponInfo(Session),
                EquipGuid = this.Guid,
                EquipType = (uint)EquipType.EQUIP_WEAPON,
                ItemId = this.WeaponId
			};
            Session!.SendPacket(ntf);
		}
	}

	// returns (maxPromoteLevel, maxWeaponLevel)
	public (uint, uint) GetMaxWeaponPromote(WeaponExcelConfig weaponExcel)
    {
        uint promoteId = (uint)weaponExcel.weaponPromoteId;
        Dictionary<uint, WeaponPromoteExcelConfig>? relevantPromotes = resourceManager.WeaponPromoteExcel.TryGetValue(promoteId, out var configDict) ? configDict : null;
        if (relevantPromotes == null || relevantPromotes.Count == 0)
            return (1, 1);
		WeaponPromoteExcelConfig highestPromote = relevantPromotes.Values.OrderByDescending(wp => wp.promoteLevel).First();
		return (highestPromote.promoteLevel, highestPromote.unlockMaxLevel);
	}

	public SceneWeaponInfo ToSceneWeaponInfo(Session session)
    {
        SceneWeaponInfo info = new SceneWeaponInfo()
        {
            EntityId = this.WeaponEntityId,
            GadgetId = this.GadgetId,
            ItemId = this.WeaponId,
            Guid = this.Guid,
            Level = this.Level,
            PromoteLevel = this.PromoteLevel,
            AbilityInfo = new AbilitySyncStateInfo()
        };
        if (session.player!.Scene.EntityManager.TryGet(this.WeaponEntityId, out Entity entity))
		{
		    WeaponEntity weaponEntity = (WeaponEntity)entity;
            // Populate ability info from weapon entity
            info.AbilityInfo = weaponEntity.BuildAbilityInfo();
            
            foreach (uint affixId in weaponEntity.GetAffixMap().Keys)
            {
                if (affixId == 0) continue;
                List<EquipAffixExcelConfig>? affixConfigs = resourceManager.EquipAffixExcel.Values.Where(e => e.AffixId == affixId).ToList();
                foreach (var affixConfig in affixConfigs)
                {
                    // todo: actually handle it properly later
                    info.AffixMaps[affixConfig.Id] = affixConfig.Level;
                }
            }
		}
		return info;
    }
}
