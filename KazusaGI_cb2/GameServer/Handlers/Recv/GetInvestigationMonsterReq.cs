using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGetInvestigationMonsterReq
{
    [Packet.PacketCmdId(PacketId.GetInvestigationMonsterReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        List<MonsterLua> alreadySpawned = session.player!.Scene.alreadySpawnedMonsters;
        GetInvestigationMonsterReq req = packet.GetDecodedBody<GetInvestigationMonsterReq>();
        GetInvestigationMonsterRsp rsp = new GetInvestigationMonsterRsp();
        foreach (uint cityId in req.CityIdLists)
        {
            List<InvestigationMonsterConfig> investigationMonsters = MainApp.resourceManager.InvestigationMonsterExcel.Values.Where(x => x.cityId == cityId).ToList();
            foreach (InvestigationMonsterConfig investigationMonster in investigationMonsters)
            {
                SceneGroupLua sceneGroup = Investigation.GetSceneGroupLua(investigationMonster.groupIdList[0], alreadySpawned);
                MonsterLua? monsterLua = Investigation.GetFirstMonster(investigationMonster.monsterId, sceneGroup, alreadySpawned);
                if (monsterLua == null)
                {
                    rsp.MonsterLists.Add(new InvestigationMonster()
                    {
                        Id = investigationMonster.id,
                        CityId = investigationMonster.cityId,
                        Level = MainApp.resourceManager.WorldLevelExcel[session.player!.WorldLevel].monsterLevel,
                        IsAlive = false,
                        Pos = Session.Vector3ToVector(Investigation.GetFirstMonster(investigationMonster.monsterId, sceneGroup, new List<MonsterLua>())!.pos),
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
}