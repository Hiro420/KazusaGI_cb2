using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.GameServer.Tower;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Scene;
using System;
using System.Numerics;
using static System.Collections.Specialized.BitVector32;
using static System.Net.Mime.MediaTypeNames;

namespace KazusaGI_cb2.GameServer;

public class Player
{
    private Session session { get; set; }
    public Session Session => session;
    private Logger logger = new("Player");
    public string Name { get; set; }
    public int Level { get; set; }
    public uint Uid { get; set; }
    public Dictionary<ulong, PlayerAvatar> avatarDict { get; set; }
    public Dictionary<ulong, PlayerWeapon> weaponDict { get; set; }
    public Dictionary<ulong, PlayerItem> itemDict { get; set; }
    public List<PlayerTeam> teamList { get; set; }
    public uint TeamIndex { get; set; } = 1;
    public uint SceneId { get; set; } = 3;
    public uint WorldLevel { get; set; } = 2; // i think thats the most fair until we implement reliquary and more weapons
    public Scene Scene { get; set; }
    public uint Overworld_PointId { get; set; } // for dungeons
    public Vector3 Pos { get; private set; }
    public Vector3 Rot { get; private set; } // wont actually be used except for scene tp
    public Gender PlayerGender { get; private set; } = Gender.Female;
    public TowerInstance? towerInstance { get; set; }
	public InvokeNotifier<AbilityInvokeEntry> AbilityInvNotifyList;
	public InvokeNotifier<CombatInvokeEntry> CombatInvNotifyList;
	//public InvokeNotifier<AbilityInvokeEntry> ClientAbilityInitFinishNotifyList;
    public Entity? MpLevelEntity;

    public Player(Session session, uint uid)
    {
        Name = "KazusaPS";
        Level = 60;
        Uid = uid;
        this.session = session;

        // Initialize the dictionaries, todo: automatically add everyhing
        this.avatarDict = new();
        this.weaponDict = new();
        this.teamList = new();
        this.itemDict = new();
        this.Scene = new Scene(session, this);
        this.Pos = new();
        this.Rot = new();
        AbilityInvNotifyList = new(this, typeof(AbilityInvocationsNotify));
        CombatInvNotifyList = new(this, typeof(CombatInvocationsNotify));
        //ClientAbilityInitFinishNotifyList = new(this, typeof(ClientAbilityInitFinishNotify));
    }

    public void InitTeams()
	{
		for (int i = 0; i < 4; i++) // maybe later change to use config for max teams amount
		{
			this.teamList.Add(new PlayerTeam(session));
		}
	}

    /// <summary>
    /// Flushes all pending invoke notifications (ability, combat, client ability init)
    /// Call this method periodically or when needed to send accumulated notifications
    /// </summary>
    public void FlushInvokeNotifications()
    {
        AbilityInvNotifyList.Notify();
        CombatInvNotifyList.Notify();
        //ClientAbilityInitFinishNotifyList.Notify();
    }

    public void AddAllAvatars(Session session)
    {
        foreach (KeyValuePair<uint, AvatarExcelConfig> avatarExcelRow in MainApp.resourceManager.AvatarExcel)
        {
            if (avatarExcelRow.Key >= 11000000) continue;
            PlayerAvatar playerAvatar = new(session, avatarExcelRow.Key);
            if (avatarExcelRow.Key == 10000007)
            {
                this.teamList[0] = new PlayerTeam(session, playerAvatar);
            }
            session.player!.avatarDict.Add(playerAvatar.Guid, playerAvatar);
        }
    }

    public void AddAllMaterials(Session session, bool isSilent = false)
    {
        foreach(KeyValuePair<uint, MaterialExcelConfig> materialExcelRow in MainApp.resourceManager.MaterialExcel)
        {
            if (materialExcelRow.Value.itemType != ItemType.ITEM_MATERIAL && materialExcelRow.Value.itemType != ItemType.ITEM_VIRTUAL)
                continue;
            PlayerItem playerItem = new PlayerItem(session, materialExcelRow.Key);
            session.player!.itemDict.Add(playerItem.Guid, playerItem);
            if (!isSilent)
            {
                session.SendPacket(new StoreItemChangeNotify()
                {
                    StoreType = StoreType.StorePack,
                    ItemLists = { 
                        new Item() 
                        { 
                            Guid = playerItem.Guid,
                            ItemId = playerItem.ItemId,
                            Material = new Material() { Count = playerItem.Count }
                        } 
                    }
                });
                session.SendPacket(new ItemAddHintNotify()
                {
                    Reason = 3, // pick random one cuz doesnt matter, at least for now
                    ItemLists = { new ItemHint() { Count = playerItem.Count, ItemId = playerItem.ItemId } }
                });
            }
        }
    }
    
    public AvatarEntity? FindEntityByPlayerAvatar(Session session, PlayerAvatar playerAvatar)
    {
        List<AvatarEntity> avatarEntities = session.player.Scene.EntityManager.Entities.Values
            .OfType<AvatarEntity>()
            .ToList();
        return avatarEntities.FirstOrDefault(c => c.DbInfo == playerAvatar);
    }

    public void SendPlayerEnterSceneInfoNotify(Session session)
    {
        // Ensure MP level entity exists (get-or-create semantics like hk4e)
        if (this.MpLevelEntity == null)
        {
            this.MpLevelEntity = new MpLevelEntity(session);
            session.player.Scene.EntityManager.Add(MpLevelEntity);
        }

        // Ensure team entity exists and keep its entity id stable across scene loads
        if (GetCurrentLineup().teamEntity == null)
        {
            GetCurrentLineup().teamEntity = new TeamEntity(session);
            session.player.Scene.EntityManager.Add(GetCurrentLineup().teamEntity!);
        }
        PlayerEnterSceneInfoNotify notify = new PlayerEnterSceneInfoNotify()
        {
            CurAvatarEntityId = FindEntityByPlayerAvatar(session, GetCurrentLineup().Leader)!._EntityId,
            TeamEnterInfo = new TeamEnterSceneInfo()
            {
                TeamAbilityInfo = new(),
                TeamEntityId = session.GetEntityId(ProtEntityType.ProtEntityTeam)// GetCurrentLineup().teamEntity._EntityId
			},
            MpLevelEntityInfo = new()
            {
                EntityId = this.MpLevelEntity._EntityId,
                AuthorityPeerId = 1,
                AbilityInfo = new()
            }
        };
        foreach (PlayerAvatar playerAvatar in GetCurrentLineup().Avatars)
        {
            AvatarEntity avatarentity = FindEntityByPlayerAvatar(session, playerAvatar)!;
            notify.AvatarEnterInfoes.Add(new AvatarEnterSceneInfo()
            {
                AvatarGuid = playerAvatar.Guid,
                AvatarEntityId = avatarentity._EntityId,
                WeaponGuid = playerAvatar.EquipGuid,
                WeaponEntityId = weaponDict[playerAvatar.EquipGuid].WeaponEntityId,
                AvatarAbilityInfo = new(),
                WeaponAbilityInfo = new()
            });
        }
        session.SendPacket(notify);
    }

    public void SendSyncTeamEntityNotify(Session session)
    {
        SyncTeamEntityNotify notify = new SyncTeamEntityNotify()
        {
            SceneId = session.player.SceneId,
            TeamEntityInfoLists = 
            {
                new TeamEntityInfo()
                {
                    AuthorityPeerId = 69,
                    TeamEntityId = GetCurrentLineup().teamEntity._EntityId,
                    TeamAbilityInfo = new() // todo
				}
            }
        };
        session.SendPacket(notify);
	}

    public void SendSceneTeamUpdateNotify(Session session)
    {
        SceneTeamUpdateNotify notify = new SceneTeamUpdateNotify();

        // Snapshot avatar entities once outside the loop
        List<AvatarEntity> avatarEntities = session.player!.Scene.EntityManager.Entities.Values
            .OfType<AvatarEntity>()
            .ToList();

        // foreach (var entity in avatarEntities)
        // {
        //     Console.WriteLine($"Entity ID: {entity._EntityId}, Type: {entity.GetType().Name}");
        // }

        // foreach (var entity in session.player!.Scene.EntityManager.Entities.Values)
        // {
        //     Console.WriteLine($"[?] Entity ID: {entity._EntityId}, Type: {entity.GetType().Name}");
        // }

        foreach (PlayerAvatar playerAvatar in GetCurrentLineup().Avatars)
        {
            var avatarEntity = avatarEntities.FirstOrDefault(c => c.DbInfo.Guid == playerAvatar.Guid);
            if (avatarEntity == null)
            {
                // If you want, you can log this instead of silently skipping
                logger.LogWarning($"No AvatarEntity found for guid {playerAvatar.Guid} (ID {playerAvatar.AvatarId}) in SendSceneTeamUpdateNotify.");
                continue;
            }

            notify.SceneTeamAvatarLists.Add(new SceneTeamAvatar()
            {
                AvatarGuid = playerAvatar.Guid,
                EntityId = avatarEntity._EntityId,
                AvatarInfo = playerAvatar.ToAvatarInfo(),
                PlayerUid = this.Uid,
                SceneId = session.player!.SceneId,
                SceneAvatarInfo = playerAvatar.ToSceneAvatarInfo(),
            });
        }

        session.SendPacket(notify);
    }

    public void SetRot(Vector3 rot)
    {
        this.Rot = rot;
    }

    public void TeleportToPos(Session session, Vector3 pos, bool isSilent = false)
    {
        this.Pos = pos;
        if (!isSilent)
        {
            this.EnterScene(session, this.SceneId);
        }
    }

    public void EnterScene(Session session, uint sceneId, EnterType enterType = EnterType.EnterSelf)
    {
        // mark existing ability managers as not initialized
        foreach (var entity in session.player.Scene.EntityManager.Entities.Values)
        {
            if (entity is IDamageable damageableEntity)
            {
				// remove the entity from the scene
                entity.ForceKill();
			}
		}
        
        uint oldSceneId = session.player!.SceneId;
        session.player!.Scene.isFinishInit = false;
        ResourceManager resourceManager = MainApp.resourceManager;
        Vector3 oldPos = session.player!.Pos;
        Vector3 newPos;

        resourceManager.ScenePoints.TryGetValue(sceneId, out ScenePoint? point);
        if (point == null)
        {
            logger.LogError($"Scene {sceneId} not found, please verify your resources");
            return;
        }

        // not really efficient but it works, so who cares
        if (oldPos == new Vector3())
        {
            newPos = resourceManager.SceneLuas[sceneId].scene_config.born_pos;
            session.player.Pos = newPos;
        } 
        else
        {
            newPos = oldPos;
        }

        this.SceneId = sceneId;
        // instantiate a fresh scene (and EntityManager) for the new scene id
        this.Scene = new Scene(session, this);
		this.Scene.EntityManager.Add(new SceneEntity(session));

        // re-add core player-related entities into the new scene's entity manager
        // 1) avatars in current lineup
        foreach (var avatar in GetCurrentLineup().Avatars)
        {
            var avatarEntity = new AvatarEntity(session, avatar);
            Scene.EntityManager.Add(avatarEntity);

            // ensure weapon entity exists if equipped
            //if (avatar.EquipGuid != 0 && weaponDict.TryGetValue(avatar.EquipGuid, out var playerWeapon))
            //{
            //    var weaponEntity = new WeaponEntity(session, playerWeapon.ItemId)
            //    {
            //        WeaponGuid = playerWeapon.Guid
            //    };
            //    playerWeapon.WeaponEntityId = weaponEntity._EntityId;
            //    Scene.EntityManager.Add(weaponEntity);
            //}
        }

        // 2) team entity
        if (GetCurrentLineup().teamEntity == null)
            GetCurrentLineup().teamEntity = new TeamEntity(session);
        Scene.EntityManager.Add(GetCurrentLineup().teamEntity!);

        // 3) MP level entity
        MpLevelEntity = new MpLevelEntity(session);
        Scene.EntityManager.Add(MpLevelEntity);

        PlayerEnterSceneNotify enterSceneNotify = new()
        {
            SceneId = sceneId,
            PrevSceneId = oldSceneId,
            Pos = Session.Vector3ToVector(newPos),
            SceneBeginTime = 0,
            Type = enterType,
            PrevPos = Session.Vector3ToVector(oldPos),
            EnterSceneToken = 69,
            WorldLevel = 1,
            TargetUid = this.Uid
        };
        session.SendPacket(enterSceneNotify);
    }

    public void SendAvatarDataNotify(Session session)
    {
        AvatarDataNotify dataNotify = new AvatarDataNotify()
        {
            CurAvatarTeamId = this.TeamIndex,
            ChooseAvatarGuid = GetCurrentLineup().Leader!.Guid
        };
        for (uint i = 1; i <= this.teamList.Count; i++)
        {
            PlayerTeam playerTeam = this.teamList[(int)i-1];
            AvatarTeam avatarTeam = new AvatarTeam()
            {
                TeamName = $"KazusaGI team {i}"
            };
            foreach (PlayerAvatar playerAvatar in playerTeam.Avatars)
            {
                avatarTeam.AvatarGuidLists.Add(playerAvatar.Guid);
            }

            dataNotify.AvatarTeamMaps.Add(i, avatarTeam);
        }
        foreach (KeyValuePair<ulong, PlayerAvatar> pair in this.avatarDict)
        {
            PlayerAvatar avatar = pair.Value;
            dataNotify.AvatarLists.Add(avatar.ToAvatarInfo());
        }
        session.SendPacket(dataNotify);
    }

    public PlayerTeam GetCurrentLineup()
    {
        return this.teamList[(int)this.TeamIndex-1];
    }

    public enum Gender
    {
        All = 0,
        Female = 1,
        Male = 2,
        Others = 3
    }
}
