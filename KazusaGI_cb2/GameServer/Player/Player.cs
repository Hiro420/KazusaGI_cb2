using KazusaGI_cb2.GameServer.Account;
using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.GameServer.Tower;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource.Json.Scene;
using KazusaGI_cb2.Resource.ServerExcel;
using System.Numerics;

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
	// Mirrors hk4e's PlayerSceneComp::first_trans_point_id_ and enter_first_trans_point_time_. 
	// Used when entering tower trans point regions.
	public uint FirstTransPointId { get; private set; }
	public uint EnterFirstTransPointTime { get; private set; }

	// Mirrors hk4e's PlayerAvatarComp::is_allow_use_skill_
	// Controls whether the client may use active skills.
	public bool IsAllowUseSkill { get; private set; } = true;
	// Mirrors hk4e's PlayerSceneComp::enter_scene_token_. Tracks the
	// current enter-scene token for validating EnterScene* requests.
	public uint EnterSceneToken { get; private set; }
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

	// Tracks opened one-off/persistent gadgets across sessions, keyed by
	// (SceneId, GroupId, ConfigId) to mirror hk4e's per-world gadget state.
	public HashSet<(uint SceneId, uint GroupId, uint ConfigId)> OpenedGadgets { get; } = new();

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

		// Persist opened one-off/persistent gadgets so they don't respawn
		// across sessions when marked persistent in script config.
		if (OpenedGadgets.Count > 0)
		{
			foreach (var (sceneId, groupId, configId) in OpenedGadgets)
			{
				record.OpenedGadgets.Add(new Account.OpenedGadgetSnapshot
				{
					SceneId = sceneId,
					GroupId = groupId,
					ConfigId = configId
				});
			}
		}

		for (uint i = 1; i <= teamList.Count; i++)
		{
			var team = teamList[(int)i - 1];
			if (team.Avatars.Count == 0)
				continue;

			var leader = team.Leader ?? team.Avatars[0];
			var snap = new PlayerTeamSnapshot
			{
				Index = i,
				LeaderAvatarGuid = leader.Guid,
				LeaderAvatarId = leader.AvatarId
			};
			foreach (var avatar in team.Avatars)
			{
				snap.AvatarGuids.Add(avatar.Guid);
				snap.AvatarIds.Add(avatar.AvatarId); // legacy migration data
			}
			record.Teams.Add(snap);
		}

		foreach (var item in itemDict.Values)
		{
			record.Items.Add(new PlayerItemSnapshot
			{
				Guid = item.Guid,
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
			snap.BuffIds.AddRange(avatar.BuffIds);
			snap.FetterExpNumber = avatar.FetterExpNumber;
			snap.FetterLevel = avatar.FetterLevel;
			snap.FetterOpenIds.AddRange(avatar.FetterOpenIds);
			foreach (var kv in avatar.ProudSkillExtraLevels)
				snap.ProudSkillExtraLevels[kv.Key] = kv.Value;
			foreach (var (skillId, state) in avatar.SkillStates)
			{
				var skillSnap = new PlayerAvatarSkillSnapshot
				{
					SkillId = skillId,
					PassCdTime = state.PassCdTime,
					MaxChargeCount = state.MaxChargeCount
				};
				skillSnap.FullCdTimes.AddRange(state.FullCdTimes);
				snap.SkillStates.Add(skillSnap);
			}

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
				EquipGuid = weapon.EquipGuid,
				AffixMap = new Dictionary<uint, uint>(weapon.AffixMap)
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

		OpenedGadgets.Clear();
		foreach (var opened in record.OpenedGadgets)
			OpenedGadgets.Add((opened.SceneId, opened.GroupId, opened.ConfigId));

		var avatarGuidMap = new Dictionary<ulong, ulong>();
		var weaponGuidMap = new Dictionary<ulong, ulong>();

		if (record.Avatars.Count > 0)
		{
			avatarDict.Clear();
			weaponDict.Clear();

			foreach (var a in record.Avatars)
			{
				ulong? requestedGuid = GuidMgr.IsGuidOfType(a.Guid, GuidMgr.GuidType.Avatar) ? a.Guid : null;
				var avatar = new PlayerAvatar(session, a.AvatarId, requestedGuid);
				avatarDict.Add(avatar.Guid, avatar);
				if (a.Guid != 0) avatarGuidMap[a.Guid] = avatar.Guid;

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
				avatar.RestoreSkillDepot(a.SkillDepotId, a.UltSkillId);

				avatar.SkillLevels.Clear();
				foreach (var kv in a.SkillLevels) avatar.SkillLevels[kv.Key] = kv.Value;
				avatar.UnlockedTalents = new HashSet<uint>(a.UnlockedTalents);
				avatar.ProudSkills = new HashSet<uint>(a.ProudSkills);
				avatar.BuffIds.Clear();
				avatar.BuffIds.UnionWith(a.BuffIds);
				avatar.FetterExpNumber = a.FetterExpNumber;
				avatar.FetterLevel = a.FetterLevel == 0 ? 1u : a.FetterLevel;
				avatar.FetterOpenIds.Clear();
				avatar.FetterOpenIds.UnionWith(a.FetterOpenIds);
				avatar.ProudSkillExtraLevels.Clear();
				foreach (var kv in a.ProudSkillExtraLevels)
					avatar.ProudSkillExtraLevels[kv.Key] = kv.Value;
				avatar.SkillStates.Clear();
				foreach (var state in a.SkillStates)
				{
					var runtime = new AvatarSkillRuntimeState
					{
						PassCdTime = state.PassCdTime,
						MaxChargeCount = state.MaxChargeCount
					};
					runtime.FullCdTimes.AddRange(state.FullCdTimes);
					avatar.SkillStates[state.SkillId] = runtime;
				}
			}

			if (record.Weapons.Count > 0)
			{
				weaponDict.Clear();
				foreach (var avatar in avatarDict.Values) avatar.EquipGuid = 0;
			}
		}

		if (record.Weapons.Count > 0)
		{
			if (record.Avatars.Count == 0) weaponDict.Clear();

			foreach (var w in record.Weapons)
			{
				ulong? requestedGuid = GuidMgr.IsGuidOfType(w.Guid, GuidMgr.GuidType.Item) ? w.Guid : null;
				var weapon = new PlayerWeapon(session, w.WeaponId, requestedGuid)
				{
					Level = w.Level,
					Exp = w.Exp,
					PromoteLevel = w.PromoteLevel,
					GadgetId = w.GadgetId
				};
				weapon.AffixMap.Clear();
				if (w.AffixMap != null && w.AffixMap.Count != 0)
					foreach (var kv in w.AffixMap) weapon.AffixMap[kv.Key] = kv.Value;
				else if (MainApp.resourceManager.WeaponExcel.TryGetValue(w.WeaponId, out var restoredExcel))
					foreach (uint affixId in restoredExcel.skillAffix ?? Enumerable.Empty<uint>())
						if (affixId != 0) weapon.AffixMap.TryAdd(affixId, 0);
				weapon.EquipGuid = null;
				if (w.Guid != 0) weaponGuidMap[w.Guid] = weapon.Guid;
			}

			foreach (var w in record.Weapons)
			{
				if (w.Guid == 0 || !w.EquipGuid.HasValue) continue;
				ulong actualWeaponGuid = weaponGuidMap.GetValueOrDefault(w.Guid, w.Guid);
				ulong actualAvatarGuid = avatarGuidMap.GetValueOrDefault(w.EquipGuid.Value, w.EquipGuid.Value);
				if (weaponDict.TryGetValue(actualWeaponGuid, out var weapon) &&
					avatarDict.TryGetValue(actualAvatarGuid, out var avatar))
					weapon.EquipOnAvatar(avatar, broadcastPacket: false);
			}

			foreach (var a in record.Avatars)
			{
				PlayerAvatar? avatar = null;
				if (a.Guid != 0)
				{
					ulong actualAvatarGuid = avatarGuidMap.GetValueOrDefault(a.Guid, a.Guid);
					avatarDict.TryGetValue(actualAvatarGuid, out avatar);
				}
				avatar ??= avatarDict.Values.FirstOrDefault(x => x.AvatarId == a.AvatarId);
				if (avatar == null || avatar.EquipGuid != 0 || a.EquipGuid == 0) continue;

				ulong actualWeaponGuid = weaponGuidMap.GetValueOrDefault(a.EquipGuid, a.EquipGuid);
				if (weaponDict.TryGetValue(actualWeaponGuid, out var weapon))
					weapon.EquipOnAvatar(avatar, broadcastPacket: false);
			}
		}

		itemDict.Clear();
		foreach (var itemSnap in record.Items)
		{
			ulong? requestedGuid = GuidMgr.IsGuidOfType(itemSnap.Guid, GuidMgr.GuidType.Item) ? itemSnap.Guid : null;
			var item = new PlayerItem(session, itemSnap.ItemId, requestedGuid) { Count = itemSnap.Count };
			if (!itemDict.TryAdd(item.Guid, item))
				throw new InvalidOperationException($"duplicate PACK item guid {item.Guid}");
		}

		if (record.Teams.Count > 0)
		{
			teamList.Clear();
			for (int i = 0; i < 4; i++) teamList.Add(new PlayerTeam(session));

			foreach (var snap in record.Teams)
			{
				if (snap.Index == 0 || snap.Index > teamList.Count) continue;
				var team = teamList[(int)snap.Index - 1];

				if (snap.AvatarGuids.Count > 0)
				{
					foreach (ulong storedGuid in snap.AvatarGuids)
					{
						ulong guid = avatarGuidMap.GetValueOrDefault(storedGuid, storedGuid);
						if (avatarDict.TryGetValue(guid, out var avatar) && !team.Avatars.Contains(avatar))
							team.Avatars.Add(avatar);
					}
				}
				else
				{
					foreach (uint avatarId in snap.AvatarIds)
					{
						var avatar = avatarDict.Values.FirstOrDefault(a => a.AvatarId == avatarId);
						if (avatar != null && !team.Avatars.Contains(avatar)) team.Avatars.Add(avatar);
					}
				}

				if (team.Avatars.Count == 0) continue;
				ulong leaderGuid = avatarGuidMap.GetValueOrDefault(snap.LeaderAvatarGuid, snap.LeaderAvatarGuid);
				team.Leader = leaderGuid != 0 ? team.Avatars.FirstOrDefault(a => a.Guid == leaderGuid) : null;
				team.Leader ??= team.Avatars.FirstOrDefault(a => a.AvatarId == snap.LeaderAvatarId) ?? team.Avatars[0];
			}
		}
		else if (record.Avatars.Count > 0)
		{
			teamList.Clear();
			for (int i = 0; i < 4; i++) teamList.Add(new PlayerTeam(session));
			var initial = avatarDict.Values.Take(4).ToList();
			teamList[0].Avatars.AddRange(initial);
			teamList[0].Leader = initial.FirstOrDefault();
			TeamIndex = 1;
		}

		if (TeamIndex == 0 || TeamIndex > teamList.Count || GetCurrentLineup().Avatars.Count == 0)
		{
			int firstValid = teamList.FindIndex(t => t.Avatars.Count > 0);
			TeamIndex = firstValid >= 0 ? (uint)(firstValid + 1) : 1;
		}

		foreach (var avatar in avatarDict.Values)
			if (avatar.EquipGuid != 0 && weaponDict.ContainsKey(avatar.EquipGuid))
				avatar.ReCalculateFightProps();
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


	public void AddAllAvatars(uint level = 1)
	{
		foreach (KeyValuePair<uint, AvatarExcelConfig> avatarExcelRow in MainApp.resourceManager.AvatarExcel)
		{
			AvatarRow serverExcel = MainApp.resourceManager.ServerAvatarRows.First(r => r.Id == avatarExcelRow.Key);
			if (avatarExcelRow.Key == 10000007 || avatarExcelRow.Key == 10000005 || avatarExcelRow.Key >= 11000000) continue;
			if (session.player.avatarDict.Values.Any(i => i.AvatarId == serverExcel.Id)) continue;
			PlayerAvatar playerAvatar = new(session, avatarExcelRow.Key);
			playerAvatar.Level = level;
			// todo: un-hardcode
			if (level == 20)
				playerAvatar.BreakLevel = 1;
			else if (level < 40)
				playerAvatar.BreakLevel = 2;
			else if (level < 60)
				playerAvatar.BreakLevel = 3;
			else if (level < 80)
				playerAvatar.BreakLevel = 4;
			else
				playerAvatar.BreakLevel = 5;
			playerAvatar.PromoteLevel = 6;
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

			// PlayerWeapon constructor adds the persistent item to PACK; Scene creates runtime entities on demand
			var weapon = new PlayerWeapon(session, weaponExcelRow.Key);
			(weapon.PromoteLevel, weapon.Level) = weapon.GetMaxWeaponPromote(weaponExcelRow.Value);
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
		foreach (KeyValuePair<uint, MaterialExcelConfig> materialExcelRow in MainApp.resourceManager.MaterialExcel)
		{
			if (materialExcelRow.Value.itemType != ItemType.ITEM_MATERIAL && materialExcelRow.Value.itemType != ItemType.ITEM_VIRTUAL)
				continue;
			PlayerItem playerItem = new PlayerItem(session, materialExcelRow.Key)
			{
				Count = materialExcelRow.Value.stackLimit
			};
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
		=> session.player!.Scene.TryGetAvatarEntity(playerAvatar.Guid, out var entity) ? entity : null;

	private TeamEntity EnsureTeamEntityInCurrentScene(Session session)
		=> session.player!.Scene.GetOrCreateTeamEntity();

	public void SendPlayerEnterSceneInfoNotify(Session session)
	{
		var scene = session.player!.Scene;
		scene.InitializePlayerRuntimeEntities();

		PlayerAvatar leader = GetCurrentLineup().Leader
			?? throw new InvalidOperationException("current avatar team has no leader");
		AvatarEntity leaderEntity = scene.GetOrCreateAvatarEntity(leader);
		TeamEntity teamEntity = scene.GetOrCreateTeamEntity();
		MpLevelEntity mpLevelEntity = scene.GetOrCreateMpLevelEntity();

		var notify = new PlayerEnterSceneInfoNotify
		{
			CurAvatarEntityId = leaderEntity._EntityId,
			TeamEnterInfo = new TeamEnterSceneInfo
			{
				TeamAbilityInfo = teamEntity.BuildAbilityInfo(),
				TeamEntityId = teamEntity._EntityId
			},
			MpLevelEntityInfo = new MPLevelEntityInfo
			{
				EntityId = mpLevelEntity._EntityId,
				AuthorityPeerId = PeerId,
				AbilityInfo = mpLevelEntity.BuildAbilityInfo()
			}
		};

		foreach (PlayerAvatar avatar in GetCurrentLineup().Avatars)
			notify.AvatarEnterInfoes.Add(avatar.ToAvatarEnterSceneInfo(scene));

		session.SendPacket(notify);
	}

	public void SendSyncTeamEntityNotify(Session session)
	{
		if (!session.isMpSession)
			return;

		TeamEntity teamEntity = EnsureTeamEntityInCurrentScene(session);
		var notify = new SyncTeamEntityNotify
		{
			SceneId = SceneId,
			TeamEntityInfoLists =
			{
				new TeamEntityInfo
				{
					AuthorityPeerId = PeerId,
					TeamEntityId = teamEntity._EntityId,
					TeamAbilityInfo = teamEntity.BuildAbilityInfo()
				}
			}
		};
		session.SendPacket(notify);
	}

	public void SendSceneTeamUpdateNotify(Session session)
	{
		var notify = new SceneTeamUpdateNotify();
		var scene = session.player!.Scene;

		foreach (PlayerAvatar avatar in GetCurrentLineup().Avatars)
		{
			AvatarEntity avatarEntity = scene.GetOrCreateAvatarEntity(avatar);
			notify.SceneTeamAvatarLists.Add(new SceneTeamAvatar
			{
				AvatarGuid = avatar.Guid,
				EntityId = avatarEntity._EntityId,
				AvatarInfo = avatar.ToAvatarInfo(),
				PlayerUid = Uid,
				SceneId = SceneId,
				SceneAvatarInfo = avatar.ToSceneAvatarInfo(),
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
		// hk4e delAvatarAndWeaponEntity clears transient entity ids before a
		// new scene allocates replacements.
		session.player.Scene.ClearPlayerRuntimeEntities();
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
		// Runtime entity ids are scene-local in hk4e. A scene transition drops
		// every old GUID->entity binding and reconstructs fresh runtime objects.
		this.Scene = new Scene(session, this);
		this.Scene.InitializePlayerRuntimeEntities();
		this.Scene.GetOrCreateMpLevelEntity().Position = newPos;

		// Generate a new non-zero enter-scene token for this transition,
		// mirroring hk4e's monotonic PlayerSceneComp::enter_scene_token_.
		EnterSceneToken = EnterSceneToken != 0 ? EnterSceneToken + 1 : 1;

		PlayerEnterSceneNotify enterSceneNotify = new()
		{
			SceneId = sceneId,
			PrevSceneId = oldSceneId,
			Pos = Session.Vector3ToVector(newPos),
			SceneBeginTime = 0,
			Type = enterType,
			PrevPos = Session.Vector3ToVector(oldPos),
			EnterSceneToken = EnterSceneToken,
			WorldLevel = session.player.WorldLevel,
			TargetUid = this.Uid,
			DungeonId = resourceManager.DungeonExcel.Values.FirstOrDefault(d => d.sceneId == sceneId)?.id ?? 0
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
			PlayerTeam playerTeam = this.teamList[(int)i - 1];
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
		return this.teamList[(int)this.TeamIndex - 1];
	}

	public enum Gender
	{
		All = 0,
		Female = 1,
		Male = 2,
		Others = 3
	}
}
