using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandlePlayerLoginReq
{
    [Packet.PacketCmdId(PacketId.PlayerLoginReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        PlayerLoginReq req = packet.GetDecodedBody<PlayerLoginReq>();
        PlayerLoginRsp rsp = new PlayerLoginRsp()
        {
            GameBiz = "hk4e",
            IsUseAbilityHash = true,
            //IsNewPlayer = true,
            TargetUid = session.player!.Uid,
            AbilityHashCode = 2004869408, // todo: figure out
        };

        Vector3 targetPos = MainApp.resourceManager.SceneLuas[session.player.SceneId].scene_config.born_pos;
        session.player.TeleportToPos(session, targetPos, true);
        session.player.SetRot(MainApp.resourceManager.SceneLuas[session.player.SceneId].scene_config.born_rot);

        OpenStateUpdateNotify OpenStateUpdateNotify = new OpenStateUpdateNotify();

        foreach (Resource.OpenStateType i in Enum.GetValues(typeof(Resource.OpenStateType)))
        {
            if (i == OpenStateType.OPEN_STATE_TOWER_FIRST_ENTER) // OPEN_STATE_TOWER_FIRST_ENTER
            {
                OpenStateUpdateNotify.OpenStateMaps.Add(Convert.ToUInt32(i), 0);
                continue;
            }
            OpenStateUpdateNotify.OpenStateMaps.Add(Convert.ToUInt32(i), 1);
        }

        // todo: move the next 2 packets to inventory manager
        StoreWeightLimitNotify storeWeightLimitNotify = new StoreWeightLimitNotify()
        {
            StoreType = StoreType.StorePack,
            WeightLimit = UInt32.MaxValue - 1000
        };
        PlayerStoreNotify playerStoreNotify = new PlayerStoreNotify()
        {
            StoreType = StoreType.StorePack,
            WeightLimit = UInt32.MaxValue - 1000,
        };

        foreach (PlayerItem playerItem in session.player.itemDict.Values)
        {
            playerStoreNotify.ItemLists.Add(new Item()
            {
                Guid = playerItem.Guid,
                ItemId = playerItem.ItemId,
                Material = new Material()
                {
                    Count = playerItem.Count
                }
            });
        }

        foreach (PlayerWeapon playerWeapon in session.player.weaponDict.Values)
        {
            playerStoreNotify.ItemLists.Add(new Item()
            {
                Guid = playerWeapon.Guid,
                ItemId = playerWeapon.WeaponId,
                Equip = new Equip()
                {
                    Weapon = new Weapon()
                    {
                        Level = playerWeapon.Level,
                        Exp = playerWeapon.Exp,
                        PromoteLevel = playerWeapon.PromoteLevel,
                    }
                }
            });
        }

        PlayerDataNotify playerDataNotify = new PlayerDataNotify()
        {
            NickName = "KazusaGI",
            ServerTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            RegionId = 1
        };

        AddPropMap(PropType.PROP_IS_SPRING_AUTO_USE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_SPRING_AUTO_USE_PERCENT, 50, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_IS_FLYABLE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_IS_TRANSFERABLE, 1, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_CUR_PERSIST_STAMINA, 24000, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_MAX_STAMINA, 24000, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_LEVEL, (uint)session.player!.Level, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_EXP, 0, playerDataNotify.PropMaps);
        AddPropMap(PropType.PROP_PLAYER_HCOIN, 999999, playerDataNotify.PropMaps); // todo: get from inventory

        session.SendPacket(OpenStateUpdateNotify);
        session.SendPacket(storeWeightLimitNotify);
        session.SendPacket(playerStoreNotify);
        session.SendPacket(playerDataNotify);
        session.player.SendAvatarDataNotify(session);
        Investigation.SendInvestigationNotify(session);
        session.player.EnterScene(session, session.player.SceneId);
        session.SendPacket(rsp);
    }

    private static void AddPropMap(PropType propType, uint ival, Dictionary<uint, PropValue> keyValuePairs)
    {
        keyValuePairs.Add((uint)propType, new PropValue()
        {
            Type = (uint)propType,
            Ival = ival,
            Val = ival
        });
    }
}