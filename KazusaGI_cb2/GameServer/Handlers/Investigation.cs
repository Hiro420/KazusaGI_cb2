using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers;

public class Investigation
{
    [Packet.PacketCmdId(PacketId.GetInvestigationMonsterReq)]
    public static void HandleDungeonEntryInfoReq(Session session, Packet packet)
    {
        List<MonsterLua> alreadySpawned = session.player!.Scene.alreadySpawnedMonsters;
        GetInvestigationMonsterReq req = packet.GetDecodedBody<GetInvestigationMonsterReq>();
        GetInvestigationMonsterRsp rsp = new GetInvestigationMonsterRsp();
        foreach (uint cityId in req.CityIdLists)
        {
            List<InvestigationMonsterConfig> investigationMonsters = MainApp.resourceManager.InvestigationMonsterExcel.Values.Where(x => x.cityId == cityId).ToList();
            foreach (InvestigationMonsterConfig investigationMonster in investigationMonsters)
            {
                SceneGroupLua sceneGroup = GetSceneGroupLua(investigationMonster.groupIdList[0], alreadySpawned);
                MonsterLua? monsterLua = GetFirstMonster(investigationMonster.monsterId, sceneGroup, alreadySpawned);
                if (monsterLua == null)
                {
					rsp.MonsterLists.Add(new InvestigationMonster()
					{
						Id = investigationMonster.id,
						CityId = investigationMonster.cityId,
						Level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel,
						IsAlive = false,
						Pos = Session.Vector3ToVector(GetFirstMonster(investigationMonster.monsterId, sceneGroup, new List<MonsterLua>())!.pos),
						MaxBossChestNum = 69420,
						WorldResin = 1
					});
					continue;
                }
                rsp.MonsterLists.Add(new InvestigationMonster()
                {
                    Id = investigationMonster.id,
                    CityId = investigationMonster.cityId,
                    Level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel,
                    IsAlive = true,
                    Pos = Session.Vector3ToVector(monsterLua.pos),
                    MaxBossChestNum = 69420,
                    WorldResin = 1
                });
            }
        }
        session.SendPacket(rsp);
    }

    public static void SendInvestigationNotify(Session session)
    {
        PlayerInvestigationAllInfoNotify playerInvestigationAllInfoNotify = new PlayerInvestigationAllInfoNotify();
        foreach (InvestigationConfig investigationConfig in MainApp.resourceManager.InvestigationExcel.Values)
        {
            List<InvestigationTargetConfig> investigationTargets = MainApp.resourceManager.InvestigationTargetExcel.Values
                .Where(c => c.investigationId == investigationConfig.id)
                .ToList();
            Protocol.Investigation investigationInfo = new Protocol.Investigation()
            {
                Id = investigationConfig.id,
                Progress = (uint)investigationTargets.Count,
                TotalProgress = (uint)investigationTargets.Count,
                state = Protocol.Investigation.State.RewardTaken
            };
            playerInvestigationAllInfoNotify.InvestigationLists.Add(investigationInfo);
        }
        foreach (InvestigationTargetConfig investigationTargetConfig in MainApp.resourceManager.InvestigationTargetExcel.Values)
        {
            InvestigationTarget investigationInfo = new InvestigationTarget()
            {
                InvestigationId = investigationTargetConfig.investigationId,
                QuestId = investigationTargetConfig.questId,
                state = InvestigationTarget.State.RewardTaken 
            };
            playerInvestigationAllInfoNotify.InvestigationTargetLists.Add(investigationInfo);
        }
        session.SendPacket(playerInvestigationAllInfoNotify);
    }

    public static SceneGroupLua GetSceneGroupLua(uint groupId, List<MonsterLua> alreadySpawned)
    {
        SceneLua sceneluas = MainApp.resourceManager.SceneLuas[3]; // scene id for all that thing
        return sceneluas.scene_blocks.First(x => x.Value.scene_groups.ContainsKey(groupId)).Value.scene_groups[groupId];
    }

    public static MonsterLua? GetFirstMonster(uint monsterId, SceneGroupLua groupLua, List<MonsterLua> alreadySpawned)
    {
        return groupLua.monsters.FirstOrDefault(monster => monster.monster_id == monsterId && !alreadySpawned.Contains(monster));
    }
}