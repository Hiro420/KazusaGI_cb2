using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Resources;
using System.Runtime;
using System.Text;
using static KazusaGI_cb2.Utils.Crypto;

namespace KazusaGI_cb2.GameServer.Handlers;

// WILL ORGANIZE THIS WHOLE THING LATER

public class LoginStuff
{
    [Packet.PacketCmdId(PacketId.GetPlayerTokenReq)]
    public static void HandleGetPlayerTokenReq(Session session, Packet packet)
    {
        GetPlayerTokenReq req = packet.GetDecodedBody<GetPlayerTokenReq>();
        GetPlayerTokenRsp rsp = new GetPlayerTokenRsp()
        {
            Uid = 69,
            AccountType = req.AccountType,
            AccountUid = req.AccountUid,
            Token = req.AccountToken,
            GmUid = Convert.ToUInt32(req.AccountUid),
            SecretKeySeed = Convert.ToUInt64(req.AccountUid)
        };
        session.player = new Player(session, 69);
        session.player.AddAllAvatars(session);
        session.player.AddAllMaterials(session, true);
        session.SendPacket(rsp);
        session.key = NewKey(Convert.ToUInt64(req.AccountUid));
    }

    [Packet.PacketCmdId(PacketId.EnterSceneReadyReq)]
    public static void HandleEnterSceneReadyReq(Session session, Packet packet)
    {
        EnterSceneReadyReq req = packet.GetDecodedBody<EnterSceneReadyReq>();
        EnterScenePeerNotify rsp = new EnterScenePeerNotify()
        {
            PeerId = 1,
            HostPeerId = 1,
            DestSceneId = session.player!.SceneId
        };

        session.SendPacket(rsp);
        session.SendPacket(new EnterSceneReadyRsp());
    }

    [Packet.PacketCmdId(PacketId.PathfindingEnterSceneReq)]
    public static void HandlePathfindingEnterSceneReq(Session session, Packet packet)
    {
        PathfindingEnterSceneReq req = packet.GetDecodedBody<PathfindingEnterSceneReq>();
        session.SendPacket(new PathfindingEnterSceneRsp());
    }

    [Packet.PacketCmdId(PacketId.EnterSceneDoneReq)]
    public static void HandleEnterSceneDoneReq(Session session, Packet packet)
    {
        EnterSceneDoneReq req = packet.GetDecodedBody<EnterSceneDoneReq>();
        SceneEntityAppearNotify sceneEntityAppearNotify = new SceneEntityAppearNotify();
        List<AvatarEntity> avatarEntities = session.entityMap.Values
                .OfType<AvatarEntity>()
                .ToList();
        PlayerAvatar currentAvatar = session.player!.GetCurrentLineup().Leader!;
        AvatarEntity currentAvatarEntity = avatarEntities.First(c => c.DbInfo == currentAvatar);
        SceneEntityInfo entityInfo = currentAvatarEntity.ToSceneEntityInfo(session);
        sceneEntityAppearNotify.EntityLists.Add(entityInfo);
        sceneEntityAppearNotify.Param = entityInfo.EntityId;
        ScenePlayerLocationNotify scenePlayerLocationNotify = new ScenePlayerLocationNotify()
        {
            SceneId = session.player.SceneId
        };
        scenePlayerLocationNotify.PlayerLocLists.Add(new PlayerLocationInfo()
        {
            Uid = session.player.Uid,
            Pos = Session.Vector3ToVector(session.player.Pos),
            Rot = Session.Vector3ToVector(session.player.Rot),
        });
        session.SendPacket(sceneEntityAppearNotify);
        session.SendPacket(scenePlayerLocationNotify);
        session.SendPacket(new EnterSceneDoneRsp());
    }

    [Packet.PacketCmdId(PacketId.PostEnterSceneReq)]
    public static void HandlePostEnterSceneReq(Session session, Packet packet)
    {
        PostEnterSceneReq req = packet.GetDecodedBody<PostEnterSceneReq>();
        session.SendPacket(new PostEnterSceneRsp());
    }

    [Packet.PacketCmdId(PacketId.SceneInitFinishReq)]
    public static void HandleSceneInitFinishReq(Session session, Packet packet)
    {
        SceneInitFinishReq req = packet.GetDecodedBody<SceneInitFinishReq>();

        OnlinePlayerInfo onlinePlayerInfo = new OnlinePlayerInfo()
        {
            Uid = session.player!.Uid,
            Nickname = "KazusaPS",
            PlayerLevel = (uint)session.player.Level,
            AvatarId = session.player.PlayerGender == Player.Gender.Female ? (uint)10000007 : 10000005,
            CurPlayerNumInWorld = 1,
            WorldLevel = session.player.WorldLevel
        };

        WorldDataNotify worldDataNotify = new WorldDataNotify();

        worldDataNotify.WorldPropMaps.Add(1, new PropValue() { Type = 8, Ival = 8 });
        worldDataNotify.WorldPropMaps.Add(2, new PropValue() { Type = 2, Ival = 0 });

        session.SendPacket(worldDataNotify);
        session.SendPacket(new SceneDataNotify()
        {
            LevelConfigNameLists = { "Level_BigWorld" }
        });
        session.SendPacket(new HostPlayerNotify()
        {
            HostPeerId = 1,
            HostUid = session.player!.Uid
        });
        session.SendPacket(new PlayerGameTimeNotify()
        {
            GameTime = 18000,
            Uid = session.player.Uid
        });
        session.SendPacket(new SceneTimeNotify()
        {
            SceneId = session.player.SceneId,
            SceneTime = 69
        });
        session.SendPacket(new WorldPlayerInfoNotify()
        {
            PlayerInfoLists = { onlinePlayerInfo },
            PlayerUidLists = { session.player.Uid }
        });
        session.SendPacket(new ScenePlayerInfoNotify()
        {
            PlayerInfoLists =
            {
                new ScenePlayerInfo()
                {
                    Uid = session.player.Uid,
                    PeerId = 1,
                    Name = session.player.Name,
                    IsConnected = true,
                    SceneId = session.player.SceneId,
                    OnlinePlayerInfo = onlinePlayerInfo
                }
            }
        });
        session.player.SendSceneTeamUpdateNotify(session);
		session.player.SendSyncTeamEntityNotify(session);
		session.player.SendPlayerEnterSceneInfoNotify(session);
        session.SendPacket(new SceneInitFinishRsp());
        session.player.Scene.isFinishInit = true;
    }

    [Packet.PacketCmdId(PacketId.PlayerLoginReq)]
    public static void HandlePlayerLoginReq(Session session, Packet packet)
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

        foreach(PlayerItem playerItem in session.player.itemDict.Values)
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
