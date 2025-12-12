using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace KazusaGI_cb2.GameServer;

public class Scene
{
    public Session session { get; private set; }
    public Player player { get; private set; }
    public EntityManager EntityManager { get; }
    public SceneLua sceneLua { get; private set; }
    public SceneBlockLua? sceneBlockLua { get; private set; }
    private static ResourceManager resourceManager { get; } = MainApp.resourceManager;
    private static Logger logger = new("SceneManager");
    public bool isFinishInit { get; set; } = false;
    public float defaultRange { get; private set; } = 100f;

    public List<MonsterLua> alreadySpawnedMonsters { get; private set; } = new();
    public List<GadgetLua> alreadySpawnedGadgets { get; private set; } = new();
    public List<NpcLua> alreadySpawnedNpcs { get; private set; } = new();

    private readonly HashSet<int> _activeRegionIds = new(64);

    private readonly Dictionary<(SceneGroupLua Group, uint SuiteId), SuiteMembership> _suiteCache = new(64);

    private struct SuiteMembership
    {
        public HashSet<uint> Monsters;
        public HashSet<uint> Gadgets;
        public HashSet<uint> Regions;

        public static SuiteMembership From(SceneGroupLuaSuite suite)
        {
            return new SuiteMembership
            {
                Monsters = suite.monsters != null ? new HashSet<uint>(suite.monsters) : new HashSet<uint>(),
                Gadgets = suite.gadgets != null ? new HashSet<uint>(suite.gadgets) : new HashSet<uint>(),
                Regions = suite.regions != null ? new HashSet<uint>(suite.regions) : new HashSet<uint>(),
            };
        }
    }

    // todo
    public System.Action<SceneGroupLua, SceneRegionLua>? OnRegionEnter;
    public System.Action<SceneGroupLua, SceneRegionLua>? OnRegionLeave;

    private readonly List<uint> _tmpDisappearIds = new(capacity: 32);

    public Scene(Session _session, Player _player, bool newManager = false)
    {
        session = _session;
        player = _player;
        sceneLua = MainApp.resourceManager.SceneLuas[_player.SceneId];
        EntityManager = new EntityManager(session);
    }

    public void UpdateOnMove()
    {
        if (!isFinishInit) return;

        Vector3 playerPos = player.Pos;
        for (int i = 0; i < sceneLua.block_rects.Count; i++)
        {
            BlockRect blockRect = sceneLua.block_rects[i];
            SceneBlockLua blockLua = sceneLua.scene_blocks[sceneLua.blocks[i]];

            if (!IsInBlock(blockRect, playerPos))
            {
                if (blockLua == sceneBlockLua)
                {
                    UnloadSceneBlock(blockLua);
                }
                continue;
            }

            if (blockLua != sceneBlockLua)
            {
                session.c.LogWarning($"Player {player.Uid} is in range of block {sceneLua.blocks[i]}");
                LoadSceneBlock(blockLua);
            }
            else
            {
                UpdateBlock();
            }

            if (sceneBlockLua != null && sceneBlockLua.scene_groups != null)
            {
                foreach (var kv in sceneBlockLua.scene_groups)
                {
                    UpdateGroupRegions(kv.Value);
                }
            }
        }
    }

    public void UpdateBlock()
    {
        if (sceneBlockLua?.scene_groups == null) return;

        foreach (var kv in sceneBlockLua.scene_groups)
        {
            UpdateGroup(kv.Value);
        }
    }

    public void RefreshGroup(SceneGroupLua group, int suiteId)
    {
        if (group.suites == null || group.suites.Count == 0) return;
        if (suiteId <= 0 || suiteId > group.suites.Count)
            suiteId = 1;
        SceneGroupLuaSuite suite = group.suites[suiteId - 1];
        var membership = GetOrBuildSuiteMembership(group, suite);

        //Console.WriteLine($"[Scene] Refreshing group {GetGroupIdFromGroupInfo(group)} to suite {suiteId} -> {membership.Monsters.Count} monsters, {membership.Gadgets.Count} gadgets");

        // Despawn entities not in the new suite
        if (group.monsters != null)
        {
            for (int i = 0; i < group.monsters.Count; i++)
            {
                var m = group.monsters[i];
                if (!membership.Monsters.Contains(m.config_id) && alreadySpawnedMonsters.Contains(m))
                {
                    var ent = MonsterEntity2DespawnMonster(m);
                    if (ent != null)
                    {
                        _tmpDisappearIds.Add(ent._EntityId);
                        alreadySpawnedMonsters.Remove(m);
                    }
                }
            }
        }
        if (group.gadgets != null)
        {
            for (int i = 0; i < group.gadgets.Count; i++)
            {
                var g = group.gadgets[i];
                if (!membership.Gadgets.Contains(g.config_id) && alreadySpawnedGadgets.Contains(g))
                {
                    var ent = GadgetEntity2DespawnGadget(g);
                    if (ent != null)
                    {
                        _tmpDisappearIds.Add(ent._EntityId);
                        alreadySpawnedGadgets.Remove(g);
                    }
                }
            }
        }

        if (_tmpDisappearIds.Count > 0)
        {
            var disappear = new SceneEntityDisappearNotify { DisappearType = Protocol.VisionType.VisionMiss };
            for (int i = 0; i < _tmpDisappearIds.Count; i++)
            {
                uint eid = _tmpDisappearIds[i];
                disappear.EntityLists.Add(eid);
                session.SendPacket(new LifeStateChangeNotify { EntityId = eid, LifeState = 2 });
                EntityManager.Remove(eid, Protocol.VisionType.VisionMiss);
            }
            session.SendPacket(disappear);
            _tmpDisappearIds.Clear();
        }
        // Npcs are not controlled by suites, so we don't despawn them here
        // Spawn entities in the new suite
        UpdateGroup(group, suite);
    }

    private void UpdateGroupRegions(SceneGroupLua group)
    {
        SceneGroupLuaSuite baseSuite = GetBaseSuite(group);
        var membership = GetOrBuildSuiteMembership(group, baseSuite);

        if (group.regions == null || group.regions.Count == 0 || membership.Regions.Count == 0)
            return;

        Vector3 pos = player.Pos;

        for (int i = 0; i < group.regions.Count; i++)
        {
            SceneRegionLua region = group.regions[i];
            int id = (int)region.config_id;

            if (!membership.Regions.Contains((uint)id))
                continue;

            bool inside = IsInsideRegion(region, pos);
            bool wasInside = _activeRegionIds.Contains(id);

            if (inside)
            {
                if (!wasInside)
                {
                    _activeRegionIds.Add(id);
                    //OnRegionEnter?.Invoke(group, region);
                    TriggerRegionEvent(group, region, enter:true);
                }
            }
            else
            {
                if (wasInside)
                {
                    _activeRegionIds.Remove(id);
                    //OnRegionLeave?.Invoke(group, region);
                    TriggerRegionEvent(group, region, enter:false);
                }
            }
        }
    }

    private SuiteMembership GetOrBuildSuiteMembership(SceneGroupLua group, SceneGroupLuaSuite suite)
    {
        int idx = group.suites != null ? group.suites.IndexOf(suite) : -1;
        uint suiteId = idx >= 0 ? (uint)(idx + 1) : 1u;

        if (!_suiteCache.TryGetValue((group, suiteId), out var mem))
        {
            mem = SuiteMembership.From(suite);
            _suiteCache[(group, suiteId)] = mem;
        }
        return mem;
    }

    private static bool IsInsideRegion(SceneRegionLua r, in Vector3 p)
    {
        Vector3 c = r.pos;
        string s = r.shape.ToString().ToLowerInvariant() ?? "sphere";

        float dx = p.X - c.X;
        float dy = p.Y - c.Y;
        float dz = p.Z - c.Z;

        switch (s)
        {
            case "sphere":
                {
                    float rr = r.radius;
                    if (rr <= 0f && r.size != default) rr = 0.5f * r.size.X;
                    rr = rr <= 0f ? 1f : rr;
                    return (dx * dx + dy * dy + dz * dz) <= rr * rr;
                }
            //case "cubic":
            default:
                {
                    float rr = r.radius;
                    if (rr <= 0f && r.size != default) rr = 0.5f * r.size.X;
                    rr = rr <= 0f ? 1f : rr;
                    return (dx * dx + dy * dy + dz * dz) <= rr * rr;
                }
        }
    }

    private void TriggerRegionEvent(SceneGroupLua group, SceneRegionLua region, bool enter)
    {
         var args = new ScriptArgs((int)GetGroupIdFromGroupInfo(group),
             enter ? (int)TriggerEventType.EVENT_ENTER_REGION : (int)TriggerEventType.EVENT_LEAVE_REGION,
             (int)region.config_id);
        LuaManager.executeTriggersLua(session, group, args);
    }


    public uint GetGroupIdFromGroupInfo(SceneGroupLua sceneGroupLua)
    {
        foreach (var kv in sceneBlockLua!.scene_groups)
            if (ReferenceEquals(kv.Value, sceneGroupLua))
                return kv.Key;
        return 0;
    }

    public SceneGroupLua? GetGroup(int groupId)
    {
        foreach (SceneLua scene in MainApp.resourceManager.SceneLuas.Values)
        {
            if (scene?.scene_blocks == null) continue;
            foreach (var block in scene.scene_blocks)
            {
                if (block.Value.scene_groups == null) continue;
                if (block.Value.scene_groups.TryGetValue((uint)groupId, out var g)) return g;
            }
        }
        return null;
    }

    public void UpdateGroup(SceneGroupLua sceneGroupLua, SceneGroupLuaSuite? suite = null)
    {
        SceneGroupLuaSuite baseSuite = suite != null ? suite : GetBaseSuite(sceneGroupLua);
        var membership = GetOrBuildSuiteMembership(sceneGroupLua, baseSuite);

        var appearBatches = new List<SceneEntityAppearNotify>();
        SceneEntityAppearNotify currentNtf = new() { AppearType = Protocol.VisionType.VisionMeet };

        if (sceneGroupLua.monsters != null && membership.Monsters.Count != 0)
        {
            //Console.WriteLine($"[Scene] Updating group {GetGroupIdFromGroupInfo(sceneGroupLua)} -> {sceneGroupLua.monsters.Count} monsters");
            for (int i = 0; i < sceneGroupLua.monsters.Count; i++)
            {
                var m = sceneGroupLua.monsters[i];

                //Console.WriteLine($"[{m.config_id}] start");

                if (!membership.Monsters.Contains(m.config_id)) continue;

                //Console.WriteLine($"[{m.config_id}] in membership");

                if (IsInRange(m.pos, player.Pos, defaultRange))
                {
                    //Console.WriteLine($"[{m.config_id}] in range");
                    if (!alreadySpawnedMonsters.Contains(m))
                    {
                        //Console.WriteLine($"[{m.config_id}] spawn");
                        uint monsterId = m.monster_id;
                        MonsterExcelConfig monster = resourceManager.MonsterExcel[monsterId];
                        var ent = new MonsterEntity(session, monsterId, m, m.pos, m.rot);
                        EntityManager.Add(ent);
                        currentNtf.EntityLists.Add(ent.ToSceneEntityInfo());
                        alreadySpawnedMonsters.Add(m);
                        if (currentNtf.EntityLists.Count >= 10)
                        {
                            appearBatches.Add(currentNtf);
                            currentNtf = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                        }
                        LuaManager.executeTriggersLua(session, sceneGroupLua, new ScriptArgs((int)GetGroupIdFromGroupInfo(sceneGroupLua), (int)TriggerEventType.EVENT_ANY_MONSTER_LIVE, (int)ent._monsterInfo!.config_id));
                    }
                }
                else
                {
                    if (alreadySpawnedMonsters.Contains(m))
                    {
                        var ent = MonsterEntity2DespawnMonster(m);
                        if (ent != null)
                        {
                            _tmpDisappearIds.Add(ent._EntityId);
                            alreadySpawnedMonsters.Remove(m);
                        }
                    }
                }
            }
        }

        if (sceneGroupLua.npcs != null)
        {
            for (int i = 0; i < sceneGroupLua.npcs.Count; i++)
            {
                var n = sceneGroupLua.npcs[i];
                if (IsInRange(n.pos, player.Pos, defaultRange))
                {
                    if (!alreadySpawnedNpcs.Contains(n))
                    {
                        uint npcId = n.npc_id;
                        var ent = new NpcEntity(session, npcId, n, n.pos, n.rot);
                        EntityManager.Add(ent);
                        currentNtf.EntityLists.Add(ent.ToSceneEntityInfo());
                        alreadySpawnedNpcs.Add(n);
                        if (currentNtf.EntityLists.Count >= 10)
                        {
                            appearBatches.Add(currentNtf);
                            currentNtf = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                        }
                    }
                }
                else
                {
                    if (alreadySpawnedNpcs.Contains(n))
                    {
                        var ent = NpcEntity2DespawnNpc(n);
                        if (ent != null)
                        {
                            _tmpDisappearIds.Add(ent._EntityId);
                            alreadySpawnedNpcs.Remove(n);
                        }
                    }
                }
            }
        }

        if (sceneGroupLua.gadgets != null && membership.Gadgets.Count != 0)
        {
            for (int i = 0; i < sceneGroupLua.gadgets.Count; i++)
            {
                var g = sceneGroupLua.gadgets[i];
                if (!membership.Gadgets.Contains(g.config_id)) continue;

                if (IsInRange(g.pos, player.Pos, defaultRange))
                {
                    if (!alreadySpawnedGadgets.Contains(g))
                    {
                        uint gid = g.gadget_id;
                        var ent = new GadgetEntity(session, gid, g, g.pos, g.rot);
                        EntityManager.Add(ent);
                        currentNtf.EntityLists.Add(ent.ToSceneEntityInfo());
                        alreadySpawnedGadgets.Add(g);
                        if (currentNtf.EntityLists.Count >= 10)
                        {
                            appearBatches.Add(currentNtf);
                            currentNtf = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                        }
                        LuaManager.executeTriggersLua(session, sceneGroupLua, new ScriptArgs((int)GetGroupIdFromGroupInfo(sceneGroupLua), (int)TriggerEventType.EVENT_GADGET_CREATE, (int)ent._gadgetLua!.config_id));
                    }
                }
                else
                {
                    if (alreadySpawnedGadgets.Contains(g))
                    {
                        var ent = GadgetEntity2DespawnGadget(g);
                        if (ent != null)
                        {
                            _tmpDisappearIds.Add(ent._EntityId);
                            alreadySpawnedGadgets.Remove(g);
                        }
                    }
                }
            }
        }

        if (currentNtf.EntityLists.Count > 0) appearBatches.Add(currentNtf);

        for (int i = 0; i < appearBatches.Count; i++)
            session.SendPacket(appearBatches[i]);

        if (_tmpDisappearIds.Count > 0)
        {
            var disappear = new SceneEntityDisappearNotify { DisappearType = Protocol.VisionType.VisionMiss };
            for (int i = 0; i < _tmpDisappearIds.Count; i++)
            {
                uint eid = _tmpDisappearIds[i];
                disappear.EntityLists.Add(eid);
                session.SendPacket(new LifeStateChangeNotify { EntityId = eid, LifeState = 2 });
                EntityManager.Remove(eid, Protocol.VisionType.VisionMiss);
            }
            session.SendPacket(disappear);
            _tmpDisappearIds.Clear();
        }
    }

    public SceneGroupLuaSuite GetBaseSuite(SceneGroupLua group)
    {
        uint suiteId = group.init_config.suite;
        if (suiteId == 0) return group.suites[0];
        return group.suites[System.Convert.ToInt32(suiteId - 1)];
    }

    public void LoadSceneBlock(SceneBlockLua blockLua)
    {
        if (sceneBlockLua == blockLua) return;

        sceneBlockLua = blockLua;
        _activeRegionIds.Clear();

        if (blockLua.scene_groups == null) return;

        foreach (var kv in blockLua.scene_groups)
            LoadSceneGroup(kv.Value);
    }

    public void UnloadSceneBlock(SceneBlockLua blockLua)
    {
        sceneBlockLua = null;
        _activeRegionIds.Clear();

        if (blockLua.scene_groups == null) return;

        foreach (var kv in blockLua.scene_groups)
            UnloadSceneGroup(kv.Value);
    }

    public void LoadSceneGroup(SceneGroupLua sceneGroupLua)
    {
        SceneGroupLuaSuite baseSuite = GetBaseSuite(sceneGroupLua);
        var membership = GetOrBuildSuiteMembership(sceneGroupLua, baseSuite);

        var appearBatches = new List<SceneEntityAppearNotify>(2);
        SceneEntityAppearNotify current = new() { AppearType = Protocol.VisionType.VisionMeet };

        if (sceneGroupLua.monsters != null && membership.Monsters.Count != 0)
        {
            for (int i = 0; i < sceneGroupLua.monsters.Count; i++)
            {
                var m = sceneGroupLua.monsters[i];
                if (!membership.Monsters.Contains(m.config_id)) continue;
                if (!IsInRange(m.pos, player.Pos, 50f) || alreadySpawnedMonsters.Contains(m)) continue;

                var ent = new MonsterEntity(session, m.monster_id, m, m.pos, m.rot);
                EntityManager.Add(ent);
                current.EntityLists.Add(ent.ToSceneEntityInfo());
                alreadySpawnedMonsters.Add(m);
                if (current.EntityLists.Count >= 10)
                {
                    appearBatches.Add(current);
                    current = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                }
                LuaManager.executeTriggersLua(session, sceneGroupLua, new ScriptArgs((int)GetGroupIdFromGroupInfo(sceneGroupLua), (int)TriggerEventType.EVENT_ANY_MONSTER_LIVE, (int)ent._monsterInfo!.config_id));
            }
        }

        if (sceneGroupLua.npcs != null)
        {
            for (int i = 0; i < sceneGroupLua.npcs.Count; i++)
            {
                var n = sceneGroupLua.npcs[i];
                if (!IsInRange(n.pos, player.Pos, 50f) || alreadySpawnedNpcs.Contains(n)) continue;

                var ent = new NpcEntity(session, n.npc_id, n, n.pos);
                EntityManager.Add(ent);
                current.EntityLists.Add(ent.ToSceneEntityInfo());
                alreadySpawnedNpcs.Add(n);
                if (current.EntityLists.Count >= 10)
                {
                    appearBatches.Add(current);
                    current = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                }
            }
        }

        if (sceneGroupLua.gadgets != null && membership.Gadgets.Count != 0)
        {
            for (int i = 0; i < sceneGroupLua.gadgets.Count; i++)
            {
                var g = sceneGroupLua.gadgets[i];
                if (!membership.Gadgets.Contains(g.config_id)) continue;
                if (!IsInRange(g.pos, player.Pos, 50f) || alreadySpawnedGadgets.Contains(g)) continue;

                var ent = new GadgetEntity(session, g.gadget_id, g, g.pos, g.rot);
                EntityManager.Add(ent);
                current.EntityLists.Add(ent.ToSceneEntityInfo());
                alreadySpawnedGadgets.Add(g);
                if (current.EntityLists.Count >= 10)
                {
                    appearBatches.Add(current);
                    current = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                }
                LuaManager.executeTriggersLua(session, sceneGroupLua, new ScriptArgs((int)GetGroupIdFromGroupInfo(sceneGroupLua), (int)TriggerEventType.EVENT_GADGET_CREATE, (int)ent._gadgetLua!.config_id));
            }
        }

        if (current.EntityLists.Count > 0) appearBatches.Add(current);
        for (int i = 0; i < appearBatches.Count; i++)
            session.SendPacket(appearBatches[i]);
    }

    public void UnloadSceneGroup(SceneGroupLua sceneGroupLua)
    {
        if (sceneGroupLua.regions != null)
        {
            for (int i = 0; i < sceneGroupLua.regions.Count; i++)
                _activeRegionIds.Remove((int)sceneGroupLua.regions[i].config_id);
        }

        var disappear = new SceneEntityDisappearNotify { DisappearType = Protocol.VisionType.VisionMiss };

        if (sceneGroupLua.monsters != null)
        {
            for (int i = 0; i < sceneGroupLua.monsters.Count; i++)
            {
                var ent = MonsterEntity2DespawnMonster(sceneGroupLua.monsters[i]);
                if (ent != null)
                {
                    disappear.EntityLists.Add(ent._EntityId);
                    EntityManager.Remove(ent._EntityId, Protocol.VisionType.VisionMiss);
                }
            }
        }
        if (sceneGroupLua.gadgets != null)
        {
            for (int i = 0; i < sceneGroupLua.gadgets.Count; i++)
            {
                var ent = GadgetEntity2DespawnGadget(sceneGroupLua.gadgets[i]);
                if (ent != null)
                {
                    disappear.EntityLists.Add(ent._EntityId);
                    EntityManager.Remove(ent._EntityId, Protocol.VisionType.VisionMiss);
                }
            }
        }

        if (disappear.EntityLists.Count > 0)
        {
            for (int i = 0; i < disappear.EntityLists.Count; i++)
            {
                uint eid = disappear.EntityLists[i];
                session.SendPacket(new LifeStateChangeNotify { EntityId = eid, LifeState = 2 });
            }
            session.SendPacket(disappear);
        }
    }

    public void GenerateParticles(int gadgetId, int Amount, Protocol.Vector pos, Protocol.Vector rot)
    {
        for (int i = 0; i < Amount; i++)
        {
			GadgetEntity gadgetEntity = new GadgetEntity(session, (uint)gadgetId, null, Session.VectorProto2Vector3(pos), Session.VectorProto2Vector3(rot));
			var ntf = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
			ntf.EntityLists.Add(gadgetEntity.ToSceneEntityInfo());
			session.SendPacket(ntf);
			EntityManager.Add(gadgetEntity);
		}
	}

	public static bool IsInRange(in Vector3 a, in Vector3 b, float range)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return (dx * dx + dy * dy + dz * dz) < (range * range);
    }

    public bool IsInBlock(in BlockRect block, in Vector3 p)
    {
        float minX = block.min.X <= block.max.X ? block.min.X : block.max.X;
        float maxX = block.min.X <= block.max.X ? block.max.X : block.min.X;
        float minZ = block.min.Z <= block.max.Z ? block.min.Z : block.max.Z;
        float maxZ = block.min.Z <= block.max.Z ? block.max.Z : block.min.Z;
        return p.X >= minX && p.X <= maxX && p.Z >= minZ && p.Z <= maxZ;
    }

    public MonsterEntity? MonsterEntity2DespawnMonster(MonsterLua m)
        => EntityManager.Entities.Values.OfType<MonsterEntity>().FirstOrDefault(x => x._monsterInfo == m);

    public NpcEntity? NpcEntity2DespawnNpc(NpcLua n)
        => EntityManager.Entities.Values.OfType<NpcEntity>().FirstOrDefault(x => x._npcInfo == n);

    public GadgetEntity? GadgetEntity2DespawnGadget(GadgetLua g)
        => EntityManager.Entities.Values.OfType<GadgetEntity>().FirstOrDefault(x => x._gadgetLua == g);

    public Entity? FindEntityByEntityId(uint entityId)
        => EntityManager.Entities.GetValueOrDefault(entityId);
}
