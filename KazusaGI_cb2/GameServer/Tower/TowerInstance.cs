using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System.Numerics;

namespace KazusaGI_cb2.GameServer.Tower;

public class TowerInstance
{
	private static ResourceManager resourceManager = MainApp.resourceManager;
	private Logger logger = new("TowerInstance");
	private TowerLevelExcelConfig curTowerLevelExcelConfig;
	public uint curFloorId; // e.g floor 12
	public uint curLevelIndex; // e.g chamber 3
	public uint monsterLevel;
	public List<uint> buffs;
	public PlayerTeam team1;
	public PlayerTeam team2;
	public uint dungeonId;
	public uint sceneId;
	public Session session;
	public Player player;
	public uint _towerPointId = 45; // for now hardcoded
	public PlayerTeam originalTeam;

	public TowerInstance(Session session, Player player)
	{
		this.session = session;
		this.player = player;
		this.buffs = new();
	}

	/*
        [Session] Received TowerTeamSelectReq {"FloorId":1001,"TowerTeamLists":[{"TowerTeamId":1,"AvatarGuidLists":[37]}]}
        [Session] Received TowerEnterLevelReq {"EnterPointId":45}
    */

	public void HandleTowerTeamSelectReq(Packet packet)
	{
		this.originalTeam = player.GetCurrentLineup();
		TowerTeamSelectReq req = packet.GetDecodedBody<TowerTeamSelectReq>();
		curFloorId = req.FloorId;
		if (req.TowerTeamLists.Count > 1)
		{
			team2 = new PlayerTeam(session)
			{
				Avatars = new List<PlayerAvatar>(),
				Leader = player.avatarDict[req.TowerTeamLists.Find(c => c.TowerTeamId == 2)!.AvatarGuidLists[0]]
			};
			foreach (ulong avatarGuid in req.TowerTeamLists.Find(c => c.TowerTeamId == 2)!.AvatarGuidLists)
			{
				team2.AddAvatar(session, player.avatarDict[avatarGuid]);
			}
		}
		;
		team1 = new PlayerTeam(session)
		{
			Avatars = new List<PlayerAvatar>(),
			Leader = player.avatarDict[req.TowerTeamLists.Find(c => c.TowerTeamId == 1)!.AvatarGuidLists[0]]
		};
		foreach (ulong avatarGuid in req.TowerTeamLists.Find(c => c.TowerTeamId == 1)!.AvatarGuidLists)
		{
			team1.AddAvatar(session, player.avatarDict[avatarGuid]);
		}
		this.curFloorId = req.FloorId;
	}

	public void HandleTowerEnterLevelReq(Packet packet)
	{
		TowerEnterLevelReq req = packet.GetDecodedBody<TowerEnterLevelReq>();
		TowerFloorExcelConfig towerFloorExcelConfig = resourceManager.TowerFloorExcel[curFloorId];
		this.curTowerLevelExcelConfig = resourceManager.TowerLevelExcel[req.EnterPointId];
		this.monsterLevel = towerFloorExcelConfig.overrideMonsterLevel;
		this.dungeonId = curTowerLevelExcelConfig.dungeonId;
		DungeonExcelConfig dungeonExcelConfig = resourceManager.DungeonExcel[dungeonId];
		this.sceneId = dungeonExcelConfig.sceneId;
		// session.player!.teamList[(int)session.player.TeamIndex] = team1; // todo: make a method + it doesnt work
		SceneLua sceneLua = resourceManager.SceneLuas[sceneId];
		Vector3 newPos = sceneLua.scene_config.born_pos;
		Vector3 newRot = sceneLua.scene_config.born_rot;
		player.TeleportToPos(session, newPos, true);
		player.SetRot(newRot);
		player.EnterScene(session, sceneId);
		session.SendPacket(new TowerEnterLevelRsp()
		{
			FloorId = curFloorId,
			LevelIndex = curLevelIndex
			// todo: add buffs
		});
	}

	public int MirrorTeamSetUp(uint towerTeamId)
	{
		logger.LogInfo($"[TowerInstance] MirrorTeamSetUp towerTeamId={towerTeamId}");

		PlayerTeam? targetTeam = null;
		if (towerTeamId == 1)
		{
			targetTeam = team1;
		}
		else if (towerTeamId == 2)
		{
			targetTeam = team2 ?? team1;
		}

		if (targetTeam == null)
		{
			logger.LogWarning("[TowerInstance] MirrorTeamSetUp called but target team is null");
			return -1;
		}

		int teamIndex = (int)player.TeamIndex - 1;
		if (teamIndex < 0 || teamIndex >= player.teamList.Count)
		{
			teamIndex = 0;
		}

		PlayerTeam previousTeam = player.teamList[teamIndex];

		// Switch the active lineup to the requested tower team.
		player.teamList[teamIndex] = targetTeam;

		// Runtime avatar/weapon ids remain owned by this Scene. Team selection
		// only ensures bindings for the newly active lineup; it is not a death.
		var scene = player.Scene;
		foreach (var avatar in targetTeam.Avatars)
		{
			scene.GetOrCreateAvatarEntity(avatar);
			if (avatar.EquipGuid != 0 && player.weaponDict.TryGetValue(avatar.EquipGuid, out var weapon))
				scene.GetOrCreateWeaponEntity(weapon);
		}

		var targetGuids = targetTeam.Avatars.Select(a => a.Guid).ToHashSet();
		foreach (var removedAvatar in previousTeam.Avatars)
			if (!targetGuids.Contains(removedAvatar.Guid))
				scene.RemoveAvatarAndWeaponEntity(removedAvatar);

		scene.GetOrCreateTeamEntity();

		// Notify the client about the updated scene team.
		player.SendSceneTeamUpdateNotify(session);

		return 0;
	}

	public void EndInstance()
	{
		int teamIndex = (int)player.TeamIndex - 1;
		if (teamIndex < 0 || teamIndex >= player.teamList.Count)
		{
			teamIndex = 0;
		}

		player.teamList[teamIndex] = originalTeam;
		player.towerInstance = null;
	}
}
