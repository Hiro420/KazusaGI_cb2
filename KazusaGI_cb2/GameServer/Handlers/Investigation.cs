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