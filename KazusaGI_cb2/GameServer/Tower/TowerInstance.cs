using KazusaGI_cb2.GameServer.PlayerInfos;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
            foreach (uint avatarGuid in req.TowerTeamLists.Find(c => c.TowerTeamId == 2)!.AvatarGuidLists)
            {
                team2.AddAvatar(session, player.avatarDict[avatarGuid]);
            }
        };
        team1 = new PlayerTeam(session)
        {
            Avatars = new List<PlayerAvatar>(),
            Leader = player.avatarDict[req.TowerTeamLists.Find(c => c.TowerTeamId == 1)!.AvatarGuidLists[0]]
        };
        foreach (uint avatarGuid in req.TowerTeamLists.Find(c => c.TowerTeamId == 1)!.AvatarGuidLists)
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

    public void EndInstance()
    {
        player.teamList[(int)player.TeamIndex] = originalTeam;
        player.towerInstance = null;
    }
}
