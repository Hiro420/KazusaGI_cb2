using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using KazusaGI_cb2.Resource;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace KazusaGI_cb2.GameServer;

// idk when ill fully implement this, too lazy kek
public class Scene
{
    public Session session { get; private set; }
    public Player player { get; private set; }
    public SceneLua sceneLua { get; private set; }
    public SceneBlockLua? sceneBlockLua { get; private set; }
    private static ResourceManager resourceManager { get; } = MainApp.resourceManager;
    private static Logger logger = new("SceneManager");
    public bool isFinishInit { get; set; } = false;
    public float defaultRange { get; private set; } = 100f;
    public List<MonsterLua> alreadySpawnedMonsters { get; private set; } = new();
    public List<GadgetLua> alreadySpawnedGadgets { get; private set; } = new();
    public List<NpcLua> alreadySpawnedNpcs { get; private set; } = new();

    public Scene(Session _session, Player _player)
    {
        this.session = _session;
        this.player = _player;
        this.sceneLua = MainApp.resourceManager.SceneLuas[_player.SceneId];
    }

    // todo: make a timeout or something, so it wont be spammed every single frame
    public void UpdateOnMove()
    {
        if (!isFinishInit)
            return;
        Vector3 playerPos = this.player.Pos;
        for (int i = 0; i < this.sceneLua.block_rects.Count; i++)
        {
            BlockRect blockRect = this.sceneLua.block_rects[i];
            SceneBlockLua blockLua = this.sceneLua.scene_blocks[this.sceneLua.blocks[i]];

            if (!isInRange(blockRect, playerPos))
            {
                if (blockLua == sceneBlockLua)
                {
                    this.UnloadSceneBlock(blockLua);
                }
                continue;
            }

            if (blockLua != sceneBlockLua)
            {
                session.c.LogWarning($"Player {player.Uid} is in range of block {this.sceneLua.blocks[i]}");
                this.LoadSceneBlock(blockLua);
            } else
            {
                UpdateBlock();
            }

            // check entities isInRange later
            // same with regions
        }
    }

    public void UpdateBlock()
    {
        Vector3 playerPos = this.player.Pos;
        if (this.sceneBlockLua!.scene_groups != null)
        {
            foreach (var group in this.sceneBlockLua.scene_groups)
            {
                UpdateGroup(group.Value);
            }
        }
    }

    public uint GetGroupIdFromGroupInfo(SceneGroupLua sceneGroupLua)
    {
        return this.sceneBlockLua!.scene_groups.First(c => c.Value == sceneGroupLua).Key;
    }

    public SceneGroupLua? GetGroup(int groupId)
    {
        foreach (SceneLua scene in MainApp.resourceManager.SceneLuas.Values)
        {
            if (scene == null || scene.scene_blocks == null)
                continue;
            foreach (var block in scene.scene_blocks)
            {
                if (block.Value.scene_groups == null)
                    continue;
                foreach (var group in block.Value.scene_groups)
                {
                    if (group.Key == groupId)
                    {
                        return group.Value;
                    }
                }
            }
        }
        return null;
    }


    public void UpdateGroup(SceneGroupLua sceneGroupLua)
    {
        SceneGroupLuaSuite baseSuite = GetBaseSuite(sceneGroupLua);
        List<SceneEntityAppearNotify> sceneEntityAppearNotifies = new List<SceneEntityAppearNotify>();

        SceneEntityAppearNotify currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
        {
            AppearType = VisionType.VisionMeet,
        };


        // we will do it only one packet for now, since it doesnt have to contain much data
        SceneEntityDisappearNotify sceneEntityDisappearNotify = new SceneEntityDisappearNotify()
        {
            DisappearType = VisionType.VisionMiss,
        };

        // Process monsters
        foreach (MonsterLua monsterLua in sceneGroupLua.monsters)
        {
            if (!baseSuite.monsters.Contains(monsterLua.config_id))
                continue;
            if (isInRange(monsterLua.pos, player.Pos, defaultRange) && !this.alreadySpawnedMonsters.Contains(monsterLua))
            {
                uint MonsterId = monsterLua.monster_id;
                MonsterExcelConfig monster = resourceManager.MonsterExcel[MonsterId];
                MonsterEntity monsterEntity = new MonsterEntity(session, MonsterId, monsterLua, monsterLua.pos);
                session.entityMap.Add(monsterEntity._EntityId, monsterEntity);
                currentSceneEntityAppearNotify.EntityLists.Add(monsterEntity.ToSceneEntityInfo());
                alreadySpawnedMonsters.Add(monsterLua);

                // If there are more than 5 entities, push current notify and start a new one
                if (currentSceneEntityAppearNotify.EntityLists.Count >= 10)
                {
                    sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
                    currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
                    {
                        AppearType = VisionType.VisionMeet,
                    };
                }
            }
            else
            {
                if (!isInRange(monsterLua.pos, player.Pos, defaultRange) && this.alreadySpawnedMonsters.Contains(monsterLua))
                {
                    MonsterEntity? monsterEntity = this.MonsterEntity2DespawnMonster(monsterLua);
                    if (monsterEntity != null)
                    {
                        sceneEntityDisappearNotify.EntityLists.Add(monsterEntity._EntityId);
                        this.alreadySpawnedMonsters.Remove(monsterLua); // so it can respawn when we come back to the are
                    }
                }
            }
        }

        // Process Npcs
        foreach (NpcLua npcLua in sceneGroupLua.npcs)
        {
            if (isInRange(npcLua.pos, player.Pos, defaultRange) && !this.alreadySpawnedNpcs.Contains(npcLua))
            {
                uint npcId = npcLua.npc_id;
                NpcEntity npcEntity = new NpcEntity(session, npcId, npcLua, npcLua.pos);
                session.entityMap.Add(npcEntity._EntityId, npcEntity);
                currentSceneEntityAppearNotify.EntityLists.Add(npcEntity.ToSceneEntityInfo());
                alreadySpawnedNpcs.Add(npcLua);

                // If there are more than 5 entities, push current notify and start a new one
                if (currentSceneEntityAppearNotify.EntityLists.Count >= 10)
                {
                    sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
                    currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
                    {
                        AppearType = VisionType.VisionMeet,
                    };
                }
            }
            else
            {
                if (!isInRange(npcLua.pos, player.Pos, defaultRange) && this.alreadySpawnedNpcs.Contains(npcLua))
                {
                    NpcEntity? npcEntity = this.NpcEntity2DespawnNpc(npcLua);
                    if (npcEntity != null)
                    {
                        sceneEntityDisappearNotify.EntityLists.Add(npcEntity._EntityId);
                        this.alreadySpawnedNpcs.Remove(npcLua); // so it can respawn when we come back to the are
                    }
                }
            }
        }


        // Process gadgets
        foreach (GadgetLua gadgetLua in sceneGroupLua.gadgets)
        {
            if (!baseSuite.gadgets.Contains(gadgetLua.config_id))
                continue;
            if (isInRange(gadgetLua.pos, player.Pos, defaultRange) && !this.alreadySpawnedGadgets.Contains(gadgetLua))
            {
                uint GadgetID = gadgetLua.gadget_id;
                Vector3 pos = gadgetLua.pos;
                GadgetEntity gadgetEntity = new GadgetEntity(session, GadgetID, gadgetLua, pos);
                session.entityMap.Add(gadgetEntity._EntityId, gadgetEntity);
                currentSceneEntityAppearNotify.EntityLists.Add(gadgetEntity.ToSceneEntityInfo());
                alreadySpawnedGadgets.Add(gadgetLua);

                // If there are more than 5 entities, push current notify and start a new one
                if (currentSceneEntityAppearNotify.EntityLists.Count >= 10)
                {
                    sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
                    currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
                    {
                        AppearType = VisionType.VisionMeet,
                    };
                }
            }
            else
            {
                if (!isInRange(gadgetLua.pos, player.Pos, defaultRange) && this.alreadySpawnedGadgets.Contains(gadgetLua))
                {
                    GadgetEntity? gadgetEntity = this.GadgetEntity2DespawnGadget(gadgetLua);
                    if (gadgetEntity != null)
                    {
                        sceneEntityDisappearNotify.EntityLists.Add(gadgetEntity._EntityId);
                        this.alreadySpawnedGadgets.Remove(gadgetLua); // so it can respawn when we come back to the are
                    }
                }
            }
        }

        // Add the last notify if it contains any entities, we don't want to send empty notifies
        if (currentSceneEntityAppearNotify.EntityLists.Count > 0)
        {
            sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
        }

        // Send each batch of SceneEntityAppearNotify
        foreach (var notify in sceneEntityAppearNotifies)
        {
            session.SendPacket(notify);
        }

        if (sceneEntityDisappearNotify.EntityLists.Count > 0)
        {
            foreach (uint entityId in sceneEntityDisappearNotify.EntityLists)
            {
                session.SendPacket(new LifeStateChangeNotify()
                {
                    EntityId = entityId,
                    LifeState = 2,
                });
                session.entityMap.Remove(entityId);
            }
            session.SendPacket(sceneEntityDisappearNotify);
        }
    }

    public SceneGroupLuaSuite GetBaseSuite(SceneGroupLua groupLua)
    {
        uint suitId = groupLua.init_config.suite;
        if (suitId == 0)
            return groupLua.suites[0]; // idk why the fuck it exists
        int suitIndex = Convert.ToInt32(suitId - 1);
        return groupLua.suites[suitIndex];
    }

    public void LoadSceneBlock(SceneBlockLua blockLua)
    {
        if (this.sceneBlockLua == blockLua) return;
        this.sceneBlockLua = blockLua;

        // todo: validate group distance from player pos
        foreach (SceneGroupLua sceneGroupLua in blockLua.scene_groups.Values)
        {
            this.LoadSceneGroup(sceneGroupLua);
        }
    }

    public void UnloadSceneBlock(SceneBlockLua blockLua)
    {
        this.sceneBlockLua = null;
        foreach (SceneGroupLua sceneGroupLua in blockLua.scene_groups.Values)
        {
            this.UnloadSceneGroup(sceneGroupLua);
        }
    }
    public void LoadSceneGroup(SceneGroupLua sceneGroupLua)
    {
        SceneGroupLuaSuite baseSuite = GetBaseSuite(sceneGroupLua);
        List<SceneEntityAppearNotify> sceneEntityAppearNotifies = new List<SceneEntityAppearNotify>();

        SceneEntityAppearNotify currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
        {
            AppearType = VisionType.VisionMeet,
        };

        // Process monsters
        foreach (MonsterLua monsterLua in sceneGroupLua.monsters)
        {
            if (!baseSuite.monsters.Contains(monsterLua.config_id))
                continue;
            if (isInRange(monsterLua.pos, player.Pos, 50f) && !this.alreadySpawnedMonsters.Contains(monsterLua))
            {
                uint MonsterId = monsterLua.monster_id;
                MonsterExcelConfig monster = resourceManager.MonsterExcel[MonsterId];
                MonsterEntity monsterEntity = new MonsterEntity(session, MonsterId, monsterLua, monsterLua.pos);
                session.entityMap.Add(monsterEntity._EntityId, monsterEntity);
                currentSceneEntityAppearNotify.EntityLists.Add(monsterEntity.ToSceneEntityInfo());
                alreadySpawnedMonsters.Add(monsterLua);

                // If there are more than 5 entities, push current notify and start a new one
                if (currentSceneEntityAppearNotify.EntityLists.Count >= 10)
                {
                    sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
                    currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
                    {
                        AppearType = VisionType.VisionMeet,
                    };
                }
            }
            
        }

        // Process Npcs
        foreach (NpcLua npcLua in sceneGroupLua.npcs)
        {
            if (isInRange(npcLua.pos, player.Pos, 50f) && !this.alreadySpawnedNpcs.Contains(npcLua))
            {
                uint npcId = npcLua.npc_id;
                Vector3 pos = npcLua.pos;
                NpcEntity npcEntity = new NpcEntity(session, npcId, npcLua, pos);
                session.entityMap.Add(npcEntity._EntityId, npcEntity);
                currentSceneEntityAppearNotify.EntityLists.Add(npcEntity.ToSceneEntityInfo());
                alreadySpawnedNpcs.Add(npcLua);

                // If there are more than 5 entities, push current notify and start a new one
                if (currentSceneEntityAppearNotify.EntityLists.Count >= 10)
                {
                    sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
                    currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
                    {
                        AppearType = VisionType.VisionMeet,
                    };
                }
            }
        }

        // Process gadgets
        foreach (GadgetLua gadgetLua in sceneGroupLua.gadgets)
        {
            if (!baseSuite.gadgets.Contains(gadgetLua.config_id))
                continue;
            if (isInRange(gadgetLua.pos, player.Pos, 50f) && !this.alreadySpawnedGadgets.Contains(gadgetLua))
            {
                uint GadgetID = gadgetLua.gadget_id;
                Vector3 pos = gadgetLua.pos;
                GadgetEntity gadgetEntity = new GadgetEntity(session, GadgetID, gadgetLua, pos);
                session.entityMap.Add(gadgetEntity._EntityId, gadgetEntity);
                currentSceneEntityAppearNotify.EntityLists.Add(gadgetEntity.ToSceneEntityInfo());
                alreadySpawnedGadgets.Add(gadgetLua);

                // If there are more than 5 entities, push current notify and start a new one
                if (currentSceneEntityAppearNotify.EntityLists.Count >= 10)
                {
                    sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
                    currentSceneEntityAppearNotify = new SceneEntityAppearNotify()
                    {
                        AppearType = VisionType.VisionMeet,
                    };
                }
            }
        }

        // Add the last notify if it contains any entities, we don't want to send empty notifies
        if (currentSceneEntityAppearNotify.EntityLists.Count > 0)
        {
            sceneEntityAppearNotifies.Add(currentSceneEntityAppearNotify);
        }

        // Send each batch of SceneEntityAppearNotify
        foreach (var notify in sceneEntityAppearNotifies)
        {
            session.SendPacket(notify);
        }
    }


    public void UnloadSceneGroup(SceneGroupLua sceneGroupLua)
    {
        SceneEntityDisappearNotify sceneEntityDisappearNotify = new SceneEntityDisappearNotify()
        {
            DisappearType = VisionType.VisionMiss,
        };
        foreach (MonsterLua monsterLua in sceneGroupLua.monsters)
        {
            MonsterEntity? monsterEntity = this.MonsterEntity2DespawnMonster(monsterLua);
            if (monsterEntity != null)
            {
                sceneEntityDisappearNotify.EntityLists.Add(monsterEntity._EntityId);
                session.entityMap.Remove(monsterEntity._EntityId);
            }
        }
        foreach (GadgetLua gadgetLua in sceneGroupLua.gadgets)
        {
            GadgetEntity gadgetEntity = this.GadgetEntity2DespawnGadget(gadgetLua)!;
            if (gadgetEntity != null)
            {
                sceneEntityDisappearNotify.EntityLists.Add(gadgetEntity._EntityId);
                session.entityMap.Remove(gadgetEntity._EntityId);
            }
        }
        if (sceneEntityDisappearNotify.EntityLists.Count > 0)
        {
            foreach (uint entityId in sceneEntityDisappearNotify.EntityLists)
            {
                session.SendPacket(new LifeStateChangeNotify()
                {
                    EntityId = entityId,
                    LifeState = 2,
                });
                session.entityMap.Remove(entityId);
            }
            session.SendPacket(sceneEntityDisappearNotify);
        }
    }

    public MonsterEntity? MonsterEntity2DespawnMonster(MonsterLua monsterLua)
    {
        MonsterEntity? monsterEntity = session.entityMap.Values.OfType<MonsterEntity>().FirstOrDefault(x => x._monsterInfo == monsterLua);
        return monsterEntity;
    }

    public NpcEntity? NpcEntity2DespawnNpc(NpcLua npcLua)
    {
        NpcEntity? npcEntity = session.entityMap.Values.OfType<NpcEntity>().FirstOrDefault(x => x._npcInfo == npcLua);
        return npcEntity;
    }

    public GadgetEntity? GadgetEntity2DespawnGadget(GadgetLua gadgetLua)
    {
        GadgetEntity? gadgetEntity = session.entityMap.Values.OfType<GadgetEntity>().FirstOrDefault(x => x._gadgetLua == gadgetLua);
        return gadgetEntity;
    }

    public static bool isInRange(Vector3 pos1, Vector3 pos2, float range)
    {
        return Vector3.Distance(pos1, pos2) < range;
    }

    public bool isInRange(BlockRect blockPos, Vector3 playerPos)
    {
        float minX = Math.Min(blockPos.min.X, blockPos.max.X);
        float maxX = Math.Max(blockPos.min.X, blockPos.max.X);
        float minY = Math.Min(blockPos.min.Z, blockPos.max.Z);
        float maxY = Math.Max(blockPos.min.Z, blockPos.max.Z);
        return playerPos.X >= minX && playerPos.X <= maxX && playerPos.Z >= minY && playerPos.Z <= maxY;
    }
}
