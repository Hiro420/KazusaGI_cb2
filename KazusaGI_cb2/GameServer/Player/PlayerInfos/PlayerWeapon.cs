using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KazusaGI_cb2.Protocol;

namespace KazusaGI_cb2.GameServer.PlayerInfos;

public class PlayerWeapon
{
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
        WeaponEntity weaponEntity = new(session, WeaponId);
        weaponExcel = resourceManager.WeaponExcel[WeaponId];
        this.Guid = session.GetGuid();
        this.WeaponId = WeaponId;
        this.Level = 1;
        this.Exp = 0;
        this.PromoteLevel = 1;
        this.GadgetId = weaponExcel.gadgetId;
        this.WeaponEntityId = weaponEntity._EntityId;
        session.player!.weaponDict.Add(this.Guid, this);
        session.player.Scene.EntityManager.Add(weaponEntity);
    }

    public void EquipOnAvatar(PlayerAvatar avatar)
    {
        avatar.EquipGuid = this.Guid;
        this.EquipGuid = avatar.Guid;

        // todo: send packet from here (maybe)
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
        return info;
    }
}
