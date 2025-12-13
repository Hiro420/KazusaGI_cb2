using KazusaGI_cb2.GameServer.Account;
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
    private static uint s_nextPeerId = 1;
    private Session session { get; set; }
    public Session Session => session;
    private Logger logger = new("Player");
    // Unique peer id for this player within the server process.
    public uint PeerId { get; set; } = 1;
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
    public HashSet<uint> ActiveDungeonWayPoints { get; } = new();
    public Vector3 Pos { get; private set; }
    public Vector3 Rot { get; private set; } // wont actually be used except for scene tp
    public Gender PlayerGender { get; private set; } = Gender.Female;
    public TowerInstance? towerInstance { get; set; }
    public Entity? MpLevelEntity;
    // Mirrors hk4e's PlayerSceneComp::first_trans_point_id_ and enter_first_trans_point_time_. 
    // Used when entering tower trans point regions.
    public uint FirstTransPointId { get; private set; }
    public uint EnterFirstTransPointTime { get; private set; }

    // Mirrors hk4e's PlayerAvatarComp::is_allow_use_skill_
    // Controls whether the client may use active skills.
    public bool IsAllowUseSkill { get; private set; } = true;
    private const bool IsDefaultGirl = true;

	public Player(Session session, uint uid)
    {
        Name = "KazusaPS";
        Level = 60;
        Uid = uid;
        this.session = session;
        PeerId = s_nextPeerId++;

        // Initialize the dictionaries, todo: automatically add everyhing
        this.avatarDict = new();
        this.weaponDict = new();
        this.teamList = new();
        this.itemDict = new();
        this.Scene = new Scene(session, this);
        this.Pos = new();
        this.Rot = new();
    }

    public void SavePersistent()
    {
        AccountManager.SavePlayerData(ToPlayerDataRecord());
    }

    public bool IsInCurrentTeam(ulong avatarGuid)
    {
        var currentTeam = GetCurrentLineup();
        return currentTeam.Avatars.Any(a => a.Guid == avatarGuid);
	}

	public PlayerDataRecord ToPlayerDataRecord()
    {
        // If for some reason teams were never initialized but we have
        // avatars, create a default team so that the DB always has a
        // valid teamList representation.
        if (teamList.Count == 0 && avatarDict.Count > 0)
        {
            InitTeams();
            var firstAvatar = avatarDict.Values.First();
            teamList[0].Avatars.Add(firstAvatar);
            teamList[0].Leader = firstAvatar;
            TeamIndex = 1;
        }

        var record = new PlayerDataRecord
        {
            PlayerUid = Uid,
            SceneId = SceneId,
            PosX = Pos.X,
            PosY = Pos.Y,
            PosZ = Pos.Z,
            TeamIndex = TeamIndex,
            Level = Level
        };

        for (uint i = 1; i <= teamList.Count; i++)
        {
            var team = teamList[(int)i - 1];
            if (team.Avatars.Count == 0)
                continue;

            var snap = new PlayerTeamSnapshot
            {
                Index = i,
                LeaderAvatarId = team.Leader?.AvatarId ?? team.Avatars[0].AvatarId
            };
            foreach (var avatar in team.Avatars)
            {
                snap.AvatarIds.Add(avatar.AvatarId);
            }
            record.Teams.Add(snap);
        }

        foreach (var item in itemDict.Values)
        {
            record.Items.Add(new PlayerItemSnapshot
            {
                ItemId = item.ItemId,
                Count = item.Count
            });
        }

        // Serialize detailed avatar state
        foreach (var avatar in avatarDict.Values)
        {
            var snap = new PlayerAvatarSnapshot
            {
                Guid = avatar.Guid,
                AvatarId = avatar.AvatarId,
                Level = avatar.Level,
                Exp = avatar.Exp,
                Hp = avatar.Hp,
                MaxHp = avatar.MaxHp,
                Def = avatar.Def,
                Atk = avatar.Atk,
                CritRate = avatar.CritRate,
                CritDmg = avatar.CritDmg,
                EM = avatar.EM,
                PromoteLevel = avatar.PromoteLevel,
                BreakLevel = avatar.BreakLevel,
                CurElemEnergy = avatar.CurElemEnergy,
                SkillDepotId = avatar.SkillDepotId,
                UltSkillId = avatar.UltSkillId,
                EquipGuid = avatar.EquipGuid
            };

            foreach (var kv in avatar.SkillLevels)
            {
                snap.SkillLevels[kv.Key] = kv.Value;
            }

            snap.UnlockedTalents.AddRange(avatar.UnlockedTalents);
            snap.ProudSkills.AddRange(avatar.ProudSkills);

            record.Avatars.Add(snap);
        }

        // Ensure weapon ↔ avatar linkage is consistent before serializing weapons.
        // For every avatar that has an equipped weapon guid, guarantee that the
        // corresponding PlayerWeapon has its EquipGuid set to the avatar guid.
        foreach (var avatar in avatarDict.Values)
        {
            if (avatar.EquipGuid != 0 && weaponDict.TryGetValue(avatar.EquipGuid, out var weapon))
            {
                if (!weapon.EquipGuid.HasValue)
                {
                    weapon.EquipGuid = avatar.Guid;
                }
            }
        }

        // Serialize weapon state
        foreach (var weapon in weaponDict.Values)
        {
            record.Weapons.Add(new PlayerWeaponSnapshot
            {
                Guid = weapon.Guid,
                WeaponId = weapon.WeaponId,
                Level = weapon.Level,
                Exp = weapon.Exp,
                PromoteLevel = weapon.PromoteLevel,
                GadgetId = weapon.GadgetId,
                EquipGuid = weapon.EquipGuid
            });
        }

        return record;
    }

    public void OnEnterFirstTransPointRegion(uint pointId)
    {
        // In hk4e this also revives all avatars and records the time the player entered the first trans point region.
        // For now we mirror the state tracking aspect so that any future tower/dungeon logic can rely on it.
        FirstTransPointId = pointId;
        EnterFirstTransPointTime = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public void OnExitFirstTransPointRegion(uint pointId)
    {
        if (FirstTransPointId == pointId)
        {
            FirstTransPointId = 0;
            EnterFirstTransPointTime = 0;
        }
    }

    public void ApplyPlayerDataRecord(PlayerDataRecord record)
    {
        SceneId = record.SceneId == 0 ? SceneId : record.SceneId;
        Level = record.Level == 0 ? Level : record.Level;
        TeamIndex = record.TeamIndex == 0 ? TeamIndex : record.TeamIndex;
        Pos = new Vector3(record.PosX, record.PosY, record.PosZ);

        // Ensure avatar roster from DB is reflected in avatarDict so
        // that team snapshots (which only store AvatarIds) can be
        // rebuilt correctly. This lets the DB model drive which
        // avatars exist and are used in teams.
        if (record.Avatars.Count > 0)
        {
            foreach (var snap in record.Avatars)
            {
                bool exists = avatarDict.Values.Any(a => a.AvatarId == snap.AvatarId);
                if (!exists)
                {
                    var avatar = new PlayerAvatar(session, snap.AvatarId);
                    avatarDict[avatar.Guid] = avatar;
                }
            }
        }

        if (record.Teams.Count > 0)
        {
            teamList.Clear();
            for (int i = 0; i < 4; i++)
            {
                teamList.Add(new PlayerTeam(session));
            }

            foreach (var snap in record.Teams)
            {
                if (snap.Index == 0 || snap.Index > teamList.Count)
                    continue;

                var team = teamList[(int)snap.Index - 1];
                foreach (var avatarId in snap.AvatarIds)
                {
                    var avatar = avatarDict.Values.FirstOrDefault(a => a.AvatarId == avatarId);
                    if (avatar != null)
                    {
                        team.Avatars.Add(avatar);
                    }
                }

                if (team.Avatars.Count > 0)
                {
                    var leader = team.Avatars.FirstOrDefault(a => a.AvatarId == snap.LeaderAvatarId) ?? team.Avatars[0];
                    team.Leader = leader;
                }
            }
        }

        if (record.Items.Count > 0)
        {
            itemDict.Clear();
            foreach (var itemSnap in record.Items)
            {
                var item = new PlayerItem(session, itemSnap.ItemId)
                {
                    Count = itemSnap.Count
                };
                itemDict[item.Guid] = item;
            }
        }

        // Restore detailed avatar state
        if (record.Avatars.Count > 0)
        {
            foreach (var a in record.Avatars)
            {
                var avatar = avatarDict.Values.FirstOrDefault(x => x.AvatarId == a.AvatarId);
                if (avatar == null)
                    continue;

                avatar.Level = a.Level;
                avatar.Exp = a.Exp;
                avatar.Hp = a.Hp;
                avatar.MaxHp = a.MaxHp;
                avatar.Def = a.Def;
                avatar.Atk = a.Atk;
                avatar.CritRate = a.CritRate;
                avatar.CritDmg = a.CritDmg;
                avatar.EM = a.EM;
                avatar.PromoteLevel = a.PromoteLevel;
                avatar.BreakLevel = a.BreakLevel;
                avatar.CurElemEnergy = a.CurElemEnergy;
                avatar.SkillDepotId = a.SkillDepotId;
                avatar.UltSkillId = a.UltSkillId;
                avatar.EquipGuid = a.EquipGuid;

                avatar.SkillLevels.Clear();
                foreach (var kv in a.SkillLevels)
                {
                    avatar.SkillLevels[kv.Key] = kv.Value;
                }

                avatar.UnlockedTalents = new HashSet<uint>(a.UnlockedTalents);
                avatar.ProudSkills = new HashSet<uint>(a.ProudSkills);
            }
        }

        // Restore weapon state and re-bind equips
        if (record.Weapons.Count > 0)
        {
            foreach (var w in record.Weapons)
            {
                // Try to find an existing weapon instance for this weaponId.
                var weapon = weaponDict.Values.FirstOrDefault(x => x.WeaponId == w.WeaponId);

                // If not present (e.g. weapons granted via commands and
                // saved previously), instantiate a new PlayerWeapon so it
                // appears in inventory and can be equipped again.
                if (weapon == null)
                {
                    weapon = new PlayerWeapon(session, w.WeaponId);
                }

                weapon.Level = w.Level;
                weapon.Exp = w.Exp;
                weapon.PromoteLevel = w.PromoteLevel;
                weapon.GadgetId = w.GadgetId;
                weapon.EquipGuid = w.EquipGuid;

                if (w.EquipGuid.HasValue)
                {
                    var avatar = avatarDict.Values.FirstOrDefault(x => x.Guid == w.EquipGuid.Value);
                    if (avatar != null)
                    {
                        avatar.EquipGuid = weapon.Guid;
                    }
                }
            }
        }
    }

    public void SetIsAllowUseSkill(bool isAllowUseSkill)
    {
        if (IsAllowUseSkill == isAllowUseSkill)
        {
            // Same value as before; hk4e only logs in this case.
            return;
        }

        IsAllowUseSkill = isAllowUseSkill;

        var notify = new CanUseSkillNotify
        {
            IsCanUseSkill = isAllowUseSkill
        };

        session.SendPacket(notify);
    }

    public void InitTeams()
	{
		for (int i = 0; i < 4; i++) // maybe later change to use config for max teams amount
		{
			this.teamList.Add(new PlayerTeam(session));
		}
	}

    public void AddBasicAvatar()
    {
        uint avatarId = IsDefaultGirl ? 10000007 : 10000005;
		PlayerAvatar playerAvatar = new(session, avatarId);
		this.teamList[0] = new PlayerTeam(session, playerAvatar);
		session.player!.avatarDict.Add(playerAvatar.Guid, playerAvatar);
	}


	public void AddAllAvatars()
    {
        foreach (KeyValuePair<uint, AvatarExcelConfig> avatarExcelRow in MainApp.resourceManager.AvatarExcel)
        {
            if (avatarExcelRow.Key >= 11000000 || avatarExcelRow.Key == 10000007  || avatarExcelRow.Key == 10000005) continue;
            PlayerAvatar playerAvatar = new(session, avatarExcelRow.Key);
            session.player!.avatarDict.Add(playerAvatar.Guid, playerAvatar);
			AvatarAddNotify addNotify = new()
            {
                Avatar = playerAvatar.ToAvatarInfo(),
                IsInTeam = false
            };

            // Ensure the avatar's initial weapon (if any) is also present
            // in the inventory store, so it shows up in the bag UI.
            if (playerAvatar.EquipGuid != 0 && session.player!.weaponDict.TryGetValue(playerAvatar.EquipGuid, out var weapon))
            {
                // Hint notify for the weapon item
                //session.SendPacket(new ItemAddHintNotify()
                //{
                //    Reason = 3,
                //    ItemLists = { new ItemHint() { Count = 1, ItemId = weapon.WeaponId } }
                //});

                // Store update for the weapon equip entry
                session.SendPacket(new StoreItemChangeNotify()
                {
                    StoreType = StoreType.StorePack,
                    ItemLists =
                    {
                        new Item()
                        {
                            Guid = weapon.Guid,
                            ItemId = weapon.WeaponId,
                            Equip = new Equip()
                            {
                                Weapon = new Weapon()
                                {
                                    Exp = weapon.Exp,
                                    Level = weapon.Level,
                                    PromoteLevel = weapon.PromoteLevel
                                }
                            }
                        }
                    }
                });
            }
			session.SendPacket(addNotify);
		}
    }

    public void AddAllWeapons()
    {
        foreach (KeyValuePair<uint, WeaponExcelConfig> weaponExcelRow in MainApp.resourceManager.WeaponExcel)
        {
            // Skip obviously invalid ids if any
            if (weaponExcelRow.Key == 0)
                continue;

            // PlayerWeapon constructor handles adding itself to weaponDict and creating the entity
            var weapon = new PlayerWeapon(session, weaponExcelRow.Key);
			session.SendPacket(new ItemAddHintNotify()
			{
				Reason = 3, // pick random one cuz doesnt matter, at least for now
				ItemLists = { new ItemHint() { Count = 1, ItemId = weaponExcelRow.Key } }
			});
            session.SendPacket(new StoreItemChangeNotify()
            {
                StoreType = StoreType.StorePack,
                ItemLists = {
                    new Item()
                    {
                        Guid = weapon.Guid,
						ItemId = weaponExcelRow.Key,
                        Equip = new Equip()
                        {
                            Weapon = new Weapon()
                            {
                                Exp = weapon.Exp,
								Level = weapon.Level,
                                PromoteLevel = weapon.PromoteLevel
							}
						}
                    }
                }
            });
		}
    }

    public void AddAllMaterials(bool isSilent = false)
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
        // Ensure team entity exists and keep its entity id stable across scene loads
        if (GetCurrentLineup().teamEntity == null)
        {
            GetCurrentLineup().teamEntity = new TeamEntity(session);
            session.player.Scene.EntityManager.Add(GetCurrentLineup().teamEntity!);
        }

        // MP level entity must be a single static entity per player
        if (this.MpLevelEntity == null)
        {
            // This should never happen if EnterScene is used correctly
            session.c.LogError("MpLevelEntity is null in SendPlayerEnterSceneInfoNotify. This indicates an initialization bug.");
            return;
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
                AuthorityPeerId = this.PeerId,
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
                    AuthorityPeerId = this.PeerId,
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
        // Save updated position and state to persistent storage
        SavePersistent();
    }

    public void EnterScene(Session session, uint sceneId, EnterType enterType = EnterType.EnterSelf)
    {
        // Despawn all entities from the old scene without invoking death logic
        // so that cleanup does NOT fire EVENT_ANY_MONSTER_DIE or other
        // OnDied-driven Lua triggers. We just send disappear notifies.
        foreach (var entity in session.player.Scene.EntityManager.Entities.Values.ToList())
        {
            if (entity is MonsterEntity || entity is GadgetEntity)
			{
				session.player.Scene.EntityManager.Remove(entity._EntityId, Protocol.VisionType.VisionMiss);
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

            // ensure weapon entity exists in the new scene if equipped
            if (avatar.EquipGuid != 0 && weaponDict.TryGetValue(avatar.EquipGuid, out var playerWeapon))
            {
                var weaponEntity = new WeaponEntity(session, playerWeapon.WeaponId);
                // update the stored WeaponEntityId so future packets and hit events
                // reference the entity that actually exists in this Scene instance
                playerWeapon.WeaponEntityId = weaponEntity._EntityId;
                Scene.EntityManager.Add(weaponEntity);
            }
        }

        // 2) team entity
        if (GetCurrentLineup().teamEntity == null)
            GetCurrentLineup().teamEntity = new TeamEntity(session);
        Scene.EntityManager.Add(GetCurrentLineup().teamEntity!);

        // 3) MP level entity: reuse the single static instance and keep its entity id stable
        if (MpLevelEntity == null)
        {
            MpLevelEntity = new MpLevelEntity(session);
        }
        MpLevelEntity.Position = newPos;
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
