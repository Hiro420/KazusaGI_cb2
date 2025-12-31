using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource;
using KazusaGI_cb2.Resource.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace KazusaGI_cb2.GameServer;

public class Scene
{
    public uint SceneId => player.SceneId;
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
    private readonly Dictionary<SceneGroupLua, uint> _groupActiveSuite = new();
    private readonly Random _random = new();

    // hk4e-style per-scene entity index used by Scene::genNewEntityId.
    // This is a simple monotonically increasing counter in the range
    // [1, 0xFFFFFF] that is combined with the ProtEntityType in the
    // high bits to form the final entity id.
    private uint _nextEntityIndex = 0;
    private readonly object _entityIdLock = new();

    private class SceneChallenge
    {
        public uint GroupId;
        public SceneGroupLua? Group;
        public uint ChallengeIndex;
        public uint ChallengeId;
        public List<uint> Params = new();
        public DateTimeOffset StartTime;
        public uint DurationSeconds;
        public uint LastNotifiedRemaining;

        // Generic progress fields (extended as needed per challenge type)
        public uint KillTarget;
        public uint KillCount;
        public uint MaxIntervalBetweenKills;
        public DateTimeOffset LastKillTime;

        public string ChallengeType = string.Empty;
    }

    private readonly Dictionary<uint, Dictionary<uint, SceneChallenge>> _groupChallenges = new();

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

    /// <summary>
    /// Mirror hk4e's Scene::genNewEntityId.
    /// In C++ this does:
    ///   next_entity_index_ = (next_entity_index_ % 0xFFFFFF) + 1;
    ///   return EntityUtils::getEntityId(type, next_entity_index_);
    /// Where EntityUtils::getEntityId(type, index) is:
    ///   return index | (type << 24);
    /// We implement the same semantics here.
    /// </summary>
    public uint GenNewEntityId(ProtEntityType type)
    {
        lock (_entityIdLock)
        {
            _nextEntityIndex++;
            if (_nextEntityIndex > 0xFFFFFF)
            {
                _nextEntityIndex = 1;
            }

            return _nextEntityIndex | ((uint)type << 24);
        }
    }

    public void TickGadgets(uint now)
    {
        // Mirror hk4e's GearComp::onUpdateTimer-style behavior by invoking
        // gadget lua OnTimer callbacks once per second with the current time.
        var gadgets = EntityManager.Entities.Values.OfType<GadgetEntity>();
        foreach (var gadget in gadgets)
        {
            try
            {
                gadget.OnTimer(now);
            }
            catch (Exception ex)
            {
                session.c.LogError($"Error occured executing Gadget Lua OnTimer for gadget {gadget._gadgetId}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// hk4e periodically broadcasts ScenePlayerLocationNotify from the
    /// scene when there are multiple players present. Our server keeps
    /// one Scene per Player, but we can still mirror the behavior for
    /// future co-op by scanning all sessions for players in the same
    /// SceneId and sending a combined notify when count &gt; 1.
    /// </summary>
    public void NotifyAllPlayerLocationIfMultiPlayer()
    {
        // If there is only one session in this scene, skip just like hk4e.
        var sessionsInScene = GameServerManager.sessions
            .Where(s => s.player != null && s.player.SceneId == this.SceneId)
            .ToList();

        if (sessionsInScene.Count > 1)
            return;

        var notify = new ScenePlayerLocationNotify
        {
            SceneId = this.SceneId
        };

        foreach (var s in sessionsInScene)
        {
            var p = s.player!;
            notify.PlayerLocLists.Add(new PlayerLocationInfo
            {
                Uid = p.Uid,
                Pos = Session.Vector3ToVector(p.Pos),
                Rot = Session.Vector3ToVector(p.Rot)
            });
        }

        // Broadcast to all players in this scene.
        foreach (var s in sessionsInScene)
        {
            s.SendPacket(notify);
        }
    }

    public void EndAllChallenges()
    {
        foreach (var perGroup in _groupChallenges.Values)
        {
            foreach (var challenge in perGroup.Values.ToList())
            {
                FinishChallengeInternal(challenge, false);
            }
        }
        _groupChallenges.Clear();
    }

    public void UpdateOnMove()
    {
        if (!isFinishInit) return;

        Vector3 playerPos = player.Pos;
        UpdateChallenges();
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

    public int RefreshGroup(SceneGroupLua group, int suiteId)
    {
        if (group.suites == null || group.suites.Count == 0)
            return -1;

        // In hk4e, ScriptLib.RefreshGroup passes suite_index = 0 to mean
        // "use default/random suite". When suiteId is 0 or out of range,
        // pick an appropriate default:
        //   - if rand_suite is set, choose a suite by rand_weight;
        //   - otherwise, fall back to init_config.suite (or 1).
        int targetSuiteId;
        if (suiteId <= 0 || suiteId > group.suites.Count)
        {
            uint initSuite = group.init_config != null ? group.init_config.suite : 0u;
            bool useRandom = group.init_config != null && group.init_config.rand_suite != 0;

            if (useRandom && group.suites.Count > 0)
            {
                uint totalWeight = 0;
                for (int i = 0; i < group.suites.Count; i++)
                    totalWeight += group.suites[i].rand_weight;

                if (totalWeight == 0)
                {
                    targetSuiteId = 1;
                }
                else
                {
                    uint ticket = (uint)_random.Next(1, (int)totalWeight + 1);
                    uint acc = 0;
                    int chosen = 1;
                    for (int i = 0; i < group.suites.Count; i++)
                    {
                        acc += group.suites[i].rand_weight;
                        if (ticket <= acc)
                        {
                            chosen = i + 1;
                            break;
                        }
                    }
                    targetSuiteId = chosen;
                }
            }
            else if (initSuite >= 1 && initSuite <= group.suites.Count)
            {
                targetSuiteId = (int)initSuite;
            }
            else
            {
                targetSuiteId = 1;
            }
        }
        else
        {
            targetSuiteId = suiteId;
        }

        SceneGroupLuaSuite suite = group.suites[targetSuiteId - 1];
        var membership = GetOrBuildSuiteMembership(group, suite);

        _groupActiveSuite[group] = (uint)targetSuiteId;

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
                EntityManager.Remove(eid, Protocol.VisionType.VisionMiss, notifyClients: false);
            }
            session.SendPacket(disappear);
            _tmpDisappearIds.Clear();
        }
        // Npcs are not controlled by suites, so we don't despawn them here
        // Spawn entities in the new suite
        UpdateGroup(group, suite);

        return 0;
    }

    private void UpdateGroupRegions(SceneGroupLua group)
    {
        SceneGroupLuaSuite baseSuite = GetBaseSuite(group);
        var membership = GetOrBuildSuiteMembership(group, baseSuite);

        if (group.regions == null || group.regions.Count == 0 || membership.Regions.Count == 0)
            return;

        uint groupId = GetGroupIdFromGroupInfo(group);

        Vector3 pos = player.Pos;

        for (int i = 0; i < group.regions.Count; i++)
        {
            SceneRegionLua region = group.regions[i];
            int id = (int)region.config_id;

            if (!membership.Regions.Contains((uint)id))
            {
                session.c.LogWarning($"[Scene] Region {id} of group {groupId} not in active suite; skipping");
                continue;
            }

            bool inside = IsInsideRegion(region, pos);
            bool wasInside = _activeRegionIds.Contains(id);

            session.c.LogWarning($"[Scene] Region check: group {groupId}, region {id}, inside={inside}, wasInside={wasInside}, playerPos=({pos.X:F2},{pos.Y:F2},{pos.Z:F2}), center=({region.pos.X:F2},{region.pos.Y:F2},{region.pos.Z:F2}), radius={region.radius:F2}, size=({region.size.X:F2},{region.size.Y:F2},{region.size.Z:F2})");

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

        float dx = p.X - c.X;
        float dy = p.Y - c.Y;
        float dz = p.Z - c.Z;

        switch (r.shape)
        {
            case Resource.Excel.LuaRegionShape.SPHERE:
                {
                    float rr = r.radius;
                    if (rr <= 0f && r.size != default) rr = 0.5f * r.size.X;
                    rr = rr <= 0f ? 1f : rr;
                    return (dx * dx + dy * dy + dz * dz) <= rr * rr;
                }
            case Resource.Excel.LuaRegionShape.CUBIC:
                {
                    // Axis-aligned box using size as full extents.
                    float hx = r.size.X > 0f ? 0.5f * r.size.X : r.radius;
                    float hy = r.size.Y > 0f ? 0.5f * r.size.Y : r.radius;
                    float hz = r.size.Z > 0f ? 0.5f * r.size.Z : r.radius;

                    if (hx <= 0f) hx = 1f;
                    if (hy <= 0f) hy = hx;
                    if (hz <= 0f) hz = hx;

                    return MathF.Abs(dx) <= hx &&
                            MathF.Abs(dy) <= hy &&
                            MathF.Abs(dz) <= hz;
                }
            default:
                {
                    // Fallback to a spherical check for any other shape.
                    float rr = r.radius;
                    if (rr <= 0f && r.size != default) rr = 0.5f * r.size.X;
                    rr = rr <= 0f ? 1f : rr;
                    return (dx * dx + dy * dy + dz * dz) <= rr * rr;
                }
        }
    }

    private void TriggerRegionEvent(SceneGroupLua group, SceneRegionLua region, bool enter)
    {
        uint groupId = GetGroupIdFromGroupInfo(group);
        session.c.LogWarning($"[Scene] Region {(enter ? "enter" : "leave")} event: group {groupId}, region {region.config_id}");

        var args = new ScriptArgs((int)groupId,
            enter ? (int)TriggerEventType.EVENT_ENTER_REGION : (int)TriggerEventType.EVENT_LEAVE_REGION,
            (int)region.config_id)
        {
            // In hk4e, evt.source_eid for region events carries the
            // region's config_id so ScriptLib.GetRegionEntityCount can
            // use it as region_eid. We mirror that here.
            source_eid = (int)region.config_id
        };
        LuaManager.executeTriggersLua(session, group, args);
    }

    public int GetRegionEntityCount(int regionConfigId, EntityType entityType)
    {
        // Currently we only track avatar presence per-region via
        // _activeRegionIds. The server hosts a single player, so the
        // count is either 0 or 1.
        if (entityType == EntityType.Avatar)
        {
            return _activeRegionIds.Contains(regionConfigId) ? 1 : 0;
        }

        // Other entity types are not yet tracked at region granularity.
        return 0;
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

    public int AddExtraGroupSuite(uint groupId, uint suiteIndex)
    {
        var group = GetGroup((int)groupId);
        if (group == null)
        {
            session.c.LogWarning($"[Scene] AddExtraGroupSuite failed: group {groupId} not found");
            return -1;
        }

        if (group.suites == null || group.suites.Count == 0)
        {
            session.c.LogWarning($"[Scene] AddExtraGroupSuite failed: group {groupId} has no suites");
            return -1;
        }

        if (suiteIndex == 0 || suiteIndex > group.suites.Count)
        {
            session.c.LogWarning($"[Scene] AddExtraGroupSuite failed: invalid suiteIndex {suiteIndex} for group {groupId}");
            return -1;
        }

        // In hk4e, Group::addExtraGroupSuite adds the contents of the suite
        // on top of existing ones without despawning. We approximate this by
        // reusing the same spawning logic as UpdateGroup but restricting the
        // membership to the target suite only.

        var suite = group.suites[(int)(suiteIndex - 1)];
        var membership = SuiteMembership.From(suite);

        var appearBatches = new List<SceneEntityAppearNotify>();
        SceneEntityAppearNotify currentNtf = new() { AppearType = Protocol.VisionType.VisionMeet };

        if (group.monsters != null && membership.Monsters.Count != 0)
        {
            for (int i = 0; i < group.monsters.Count; i++)
            {
                var m = group.monsters[i];
                if (!membership.Monsters.Contains(m.config_id))
                    continue;

                var monsterPos = m.pos;
                if (IsInRange(monsterPos, player.Pos, defaultRange) && !alreadySpawnedMonsters.Contains(m))
                {
                    uint monsterId = m.monster_id;
                    MonsterExcelConfig monster = resourceManager.MonsterExcel[monsterId];
                    var ent = new MonsterEntity(session, monsterId, m, monsterPos, m.rot);
                    EntityManager.Add(ent);
                    currentNtf.EntityLists.Add(ent.ToSceneEntityInfo());
                    alreadySpawnedMonsters.Add(m);
                    if (currentNtf.EntityLists.Count >= 10)
                    {
                        appearBatches.Add(currentNtf);
                        currentNtf = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                    }

                    LuaManager.executeTriggersLua(session, group, new ScriptArgs((int)groupId, (int)TriggerEventType.EVENT_ANY_MONSTER_LIVE, (int)ent._monsterInfo!.config_id));
                }
            }
        }

        if (group.gadgets != null && membership.Gadgets.Count != 0)
        {
            for (int i = 0; i < group.gadgets.Count; i++)
            {
                var g = group.gadgets[i];
                if (!membership.Gadgets.Contains(g.config_id))
                    continue;

                var gadgetPos = g.pos;
                if (IsInRange(gadgetPos, player.Pos, defaultRange) && !alreadySpawnedGadgets.Contains(g))
                {
                    uint gid = g.gadget_id;
                    var ent = new GadgetEntity(session, gid, g, gadgetPos, g.rot);
                    EntityManager.Add(ent);
                    currentNtf.EntityLists.Add(ent.ToSceneEntityInfo());
                    alreadySpawnedGadgets.Add(g);
                    if (currentNtf.EntityLists.Count >= 10)
                    {
                        appearBatches.Add(currentNtf);
                        currentNtf = new SceneEntityAppearNotify { AppearType = Protocol.VisionType.VisionMeet };
                    }

                    LuaManager.executeTriggersLua(session, group, new ScriptArgs((int)groupId, (int)TriggerEventType.EVENT_GADGET_CREATE, (int)ent._gadgetLua!.config_id));
                }
            }
        }

        if (currentNtf.EntityLists.Count > 0)
            appearBatches.Add(currentNtf);

        for (int i = 0; i < appearBatches.Count; i++)
            session.SendPacket(appearBatches[i]);

        return 0;
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

                var monsterPos = m.pos;
                if (IsInRange(monsterPos, player.Pos, defaultRange))
                {
                    //Console.WriteLine($"[{m.config_id}] in range");
                    if (!alreadySpawnedMonsters.Contains(m))
                    {
                        //Console.WriteLine($"[{m.config_id}] spawn");
                        uint monsterId = m.monster_id;
                        MonsterExcelConfig monster = resourceManager.MonsterExcel[monsterId];
                        var ent = new MonsterEntity(session, monsterId, m, monsterPos, m.rot);
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
                var npcPos = n.pos;
                if (IsInRange(npcPos, player.Pos, defaultRange))
                {
                    if (!alreadySpawnedNpcs.Contains(n))
                    {
                        uint npcId = n.npc_id;
                        var ent = new NpcEntity(session, npcId, n, npcPos, n.rot);
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

                var gadgetPos = g.pos;
                if (IsInRange(gadgetPos, player.Pos, defaultRange))
                {
                    // Skip gadgets that this player has already consumed and
                    // that are scripted as one-off or persistent.
                    if (player.OpenedGadgets.Contains((SceneId, g.group_id, g.config_id)) && (g.isOneoff || g.persistent))
                        continue;

                    if (!alreadySpawnedGadgets.Contains(g))
                    {
                        uint gid = g.gadget_id;
                        var ent = new GadgetEntity(session, gid, g, gadgetPos, g.rot);
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
                EntityManager.Remove(eid, Protocol.VisionType.VisionMiss, notifyClients: false);
            }
            session.SendPacket(disappear);
            _tmpDisappearIds.Clear();
        }
    }

    public SceneGroupLuaSuite GetBaseSuite(SceneGroupLua group)
    {
        // Prefer the last suite selected via RefreshGroup; otherwise fall
        // back to init_config.suite (or 1) like hk4e's Group::refresh logic.
        uint suiteId;
        if (!_groupActiveSuite.TryGetValue(group, out suiteId) || suiteId == 0 || suiteId > group.suites.Count)
        {
            suiteId = group.init_config.suite;
            if (suiteId == 0 || suiteId > group.suites.Count)
                suiteId = 1;
        }

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
            var monsterPos = m.pos;
            if (!IsInRange(monsterPos, player.Pos, 50f) || alreadySpawnedMonsters.Contains(m)) continue;

            var ent = new MonsterEntity(session, m.monster_id, m, monsterPos, m.rot);
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
            var npcPos = n.pos;
            if (!IsInRange(npcPos, player.Pos, 50f) || alreadySpawnedNpcs.Contains(n)) continue;

            var ent = new NpcEntity(session, n.npc_id, n, npcPos);
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
                var gadgetPos = g.pos;
                if (!IsInRange(gadgetPos, player.Pos, 50f) || alreadySpawnedGadgets.Contains(g)) continue;

                // Skip gadgets that this player has already consumed and
                // that are scripted as one-off or persistent.
                if (player.OpenedGadgets.Contains(((uint)SceneId, g.group_id, g.config_id)) && (g.isOneoff || g.persistent))
                    continue;

                var ent = new GadgetEntity(session, g.gadget_id, g, gadgetPos, g.rot);
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
                    EntityManager.Remove(ent._EntityId, Protocol.VisionType.VisionMiss, notifyClients: false);
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
                    EntityManager.Remove(ent._EntityId, Protocol.VisionType.VisionMiss, notifyClients: false);
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

    public void OnMonsterDie(uint groupId, uint monsterConfigId)
    {
        if (!_groupChallenges.TryGetValue(groupId, out var perGroup) || perGroup.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var challenge in perGroup.Values.ToList())
        {
            switch (challenge.ChallengeType)
            {
                // Fast kill-count challenges: defeat opponents while keeping
                // the time between kills under a limit (used by challenge id 5
                // in DungeonChallengeConfig as CHALLENGE_KILL_COUNT_FAST).
                case "CHALLENGE_KILL_COUNT_FAST":
                    HandleKillIntervalChallenge(challenge, now);
                    break;

                // Simple kill-count based challenges; hk4e wires these through
                // ChallengeCondKillCount (and variants) and drives progress via
                // param_index = 1.
                case "CHALLENGE_KILL_COUNT":
                case "CHALLENGE_KILL_COUNT_IN_TIME":
                case "CHALLENGE_KILL_COUNT_GUARD_HP":
                case "CHALLENGE_TIME_FLY":
                case "CHALLENGE_KILL_COUNT_FROZEN_LESS":
                    HandleKillCountChallenge(challenge);
                    break;
            }
        }
    }

    private void HandleKillCountChallenge(SceneChallenge challenge)
    {
        if (challenge.KillTarget == 0)
            return;

        challenge.KillCount++;

        var data = new ChallengeDataNotify
        {
            ChallengeIndex = challenge.ChallengeIndex,
            ParamIndex = 1,
            Value = challenge.KillCount
        };
        session.SendPacket(data);

        if (challenge.KillCount >= challenge.KillTarget)
        {
            FinishChallengeInternal(challenge, true);
        }
    }

    private void HandleKillIntervalChallenge(SceneChallenge challenge, DateTimeOffset now)
    {
        if (challenge.KillTarget == 0)
            return;

        // Always enforce the interval between the last kill time and now.
        if (challenge.MaxIntervalBetweenKills > 0 && challenge.LastKillTime != default)
        {
            var delta = (uint)Math.Max(0, (now - challenge.LastKillTime).TotalSeconds);
            if (delta > challenge.MaxIntervalBetweenKills)
            {
                FinishChallengeInternal(challenge, false);
                return;
            }
        }

        // Chain is still valid, count this kill and restart the interval window.
        challenge.KillCount++;
        challenge.LastKillTime = now;

        // Update kill-count progress for the HUD. In hk4e this is
        // ChallengeCondKillCount with param_index = 1.
        var data = new ChallengeDataNotify
        {
            ChallengeIndex = challenge.ChallengeIndex,
            ParamIndex = 1,
            Value = challenge.KillCount
        };
        session.SendPacket(data);

        if (challenge.KillCount >= challenge.KillTarget)
        {
            FinishChallengeInternal(challenge, true);
        }
    }

    public int BeginChallenge(uint groupId, uint challengeIndex, uint challengeId, IReadOnlyList<uint> paramList)
    {
        var group = GetGroup((int)groupId);
        if (group == null)
        {
            session.c.LogWarning($"[Challenge] BeginChallenge failed: group {groupId} not found");
            return -1;
        }

        FinishAllChallengesInGroup(groupId);

        if (!_groupChallenges.TryGetValue(groupId, out var perGroup))
        {
            perGroup = new Dictionary<uint, SceneChallenge>();
            _groupChallenges[groupId] = perGroup;
        }

        uint durationSeconds = 0;
        if (paramList.Count > 0)
        {
            uint p1 = paramList[0];
            // Default behaviour (overridden per-type below): treat p1 as a duration
            // only if it is in a sane time range. Many non-timed challenges pass a
            // large group_id as p1 (e.g. 240xxx...), which should not be interpreted
            // as seconds.
            if (p1 > 0 && p1 <= 86400)
            {
                durationSeconds = p1;
            }
        }

        var challenge = new SceneChallenge
        {
            GroupId = groupId,
            Group = group,
            ChallengeIndex = challengeIndex,
            ChallengeId = challengeId,
            Params = paramList.ToList(),
            StartTime = DateTimeOffset.UtcNow,
            DurationSeconds = durationSeconds,
            LastNotifiedRemaining = durationSeconds
        };

        // Attach metadata from DungeonChallengeConfig, if available.
        if (resourceManager.DungeonChallengeConfig.TryGetValue(challengeId, out var challCfg))
        {
            challenge.ChallengeType = challCfg.challengeType;

            // Basic per-type semantics driven by challengeType.
            switch (challCfg.challengeType)
            {
                case "CHALLENGE_KILL_COUNT_IN_TIME":
                    // In hk4e this type wires an IN_TIME condition to p1 and a
                    // KILL_COUNT condition to p3 (index 2). Mirror that here so
                    // that HUD and settle use the same kill target.
                    if (paramList.Count > 2)
                        challenge.KillTarget = paramList[2];
                    break;

                case "CHALLENGE_KILL_COUNT_GUARD_HP":
                case "CHALLENGE_KILL_COUNT":
                    // Plain kill-count challenges use p2 (index 1) as kill
                    // target in hk4e.
                    if (paramList.Count > 1)
                        challenge.KillTarget = paramList[1];
                    break;

                case "CHALLENGE_KILL_COUNT_FAST":
                    // DungeonChallengeConfig id 5 uses CHALLENGE_KILL_COUNT_FAST.
                    // Lua ActiveChallenge passes:
                    //   p1: interval between kills (seconds)
                    //   p2: group_id filter (0 = any)
                    //   p3: kill target
                    if (paramList.Count > 2)
                        challenge.KillTarget = paramList[2];
                    if (paramList.Count > 0)
                        challenge.MaxIntervalBetweenKills = paramList[0];

                    // This type is driven purely by the kill-fast window and
                    // kill count; there is no overall challenge timer like
                    // CHALLENGE_IN_TIME. Disable the generic duration-based
                    // countdown so that ParamIndex = 1 can be used for kill
                    // count, matching hk4e's ChallengeCondKillCount.
                    challenge.DurationSeconds = 0;
                    challenge.LastNotifiedRemaining = 0;

                    // Seed the last-kill timestamp so that the very first
                    // kill must also happen within the configured interval,
                    // like ChallengeCondKillFast::initChallengeCond.
                    challenge.KillCount = 0;
                    challenge.LastKillTime = DateTimeOffset.UtcNow;

                    // Initialise kill counter in HUD ("Defeat N opponent(s)").
                    // In hk4e this is param_index = 1.
                    var killData = new ChallengeDataNotify
                    {
                        ChallengeIndex = challengeIndex,
                        ParamIndex = 1,
                        Value = 0
                    };
                    session.SendPacket(killData);
                    break;

                case "CHALLENGE_TIME_FLY":
                    // In hk4e this type uses an IN_TIME condition whose time limit
                    // comes from param index 2. Mirror that by driving the local
                    // countdown timer from p3 when it looks sane.
                    if (paramList.Count > 2)
                    {
                        uint p3 = paramList[2];
                        if (p3 > 0 && p3 <= 86400)
                        {
                            challenge.DurationSeconds = p3;
                            challenge.LastNotifiedRemaining = p3;
                        }
                    }
                    break;
            }
        }

        perGroup[challengeIndex] = challenge;

        var begin = new DungeonChallengeBeginNotify
        {
            ChallengeId = challengeId,
            ChallengeIndex = challengeIndex
        };

        // Build the parameter list for the client HUD in the same way as
        // hk4e's ChallengeComp::beginChallenge does via append_notify_params:
        // each challengeType supplies an index vector picking values out of
        // param_vec by 0-based index. For types that are not explicitly
        // handled, fall back to passing the raw parameter list through.
        uint GetParamAt(int idx)
        {
            return (idx >= 0 && idx < paramList.Count) ? paramList[idx] : 0u;
        }

        var notifyParams = new List<uint>();

        if (!string.IsNullOrEmpty(challenge.ChallengeType))
        {
            switch (challenge.ChallengeType)
            {
                case "CHALLENGE_KILL_COUNT":
                    // index_vec = {1}
                    notifyParams.Add(GetParamAt(1));
                    break;

                case "CHALLENGE_KILL_COUNT_IN_TIME":
                    // index_vec = {2, 0}  -> [kill_target, time]
                    notifyParams.Add(GetParamAt(2));
                    notifyParams.Add(GetParamAt(0));
                    break;

                case "CHALLENGE_SURVIVE":
                    // index_vec = {0, 1} -> [time, extra]
                    notifyParams.Add(GetParamAt(0));
                    notifyParams.Add(GetParamAt(1));
                    break;

                case "CHALLENGE_TIME_FLY":
                    // index_vec = {1, 2}
                    notifyParams.Add(GetParamAt(1));
                    notifyParams.Add(GetParamAt(2));
                    break;

                case "CHALLENGE_KILL_COUNT_FAST":
                    // index_vec = {2, 0} -> [kill_target, time]
                    notifyParams.Add(GetParamAt(2));
                    notifyParams.Add(GetParamAt(0));
                    break;

                case "CHALLENGE_KILL_COUNT_FROZEN_LESS":
                    // index_vec = {1, 2}
                    notifyParams.Add(GetParamAt(1));
                    notifyParams.Add(GetParamAt(2));
                    break;

                case "CHALLENGE_KILL_MONSTER_IN_TIME":
                    // index_vec = {0}
                    notifyParams.Add(GetParamAt(0));
                    break;

                case "CHALLENGE_TRIGGER_IN_TIME":
                    // index_vec = {0, 3}
                    notifyParams.Add(GetParamAt(0));
                    notifyParams.Add(GetParamAt(3));
                    break;

                case "CHALLENGE_GUARD_HP":
                    // For short forms hk4e uses index_vec = {3, 0}.
                    notifyParams.Add(GetParamAt(3));
                    notifyParams.Add(GetParamAt(0));
                    break;

                default:
                    notifyParams.AddRange(paramList);
                    break;
            }
        }
        else
        {
            notifyParams.AddRange(paramList);
        }

        foreach (var p in notifyParams)
            begin.ParamLists.Add(p);

        session.SendPacket(begin);

        if (challenge.DurationSeconds > 0)
        {
            var data = new ChallengeDataNotify
            {
                ChallengeIndex = challengeIndex,
                ParamIndex = 1,
                Value = challenge.DurationSeconds
            };
            session.SendPacket(data);
        }

        return 0;
    }

    public int StopChallenge(uint groupId, uint challengeIndex, bool isSuccess)
    {
        if (!_groupChallenges.TryGetValue(groupId, out var perGroup) ||
            !perGroup.TryGetValue(challengeIndex, out var challenge))
        {
            session.c.LogWarning($"[Challenge] StopChallenge: challenge {challengeIndex} in group {groupId} not found");
            return -1;
        }

        FinishChallengeInternal(challenge, isSuccess);
        return 0;
    }

    private void FinishAllChallengesInGroup(uint groupId)
    {
        if (!_groupChallenges.TryGetValue(groupId, out var perGroup) || perGroup.Count == 0)
            return;

        var toFinish = perGroup.Values.ToList();
        foreach (var challenge in toFinish)
        {
            FinishChallengeInternal(challenge, false);
        }
    }

    private void FinishChallengeInternal(SceneChallenge challenge, bool isSuccess)
    {
        uint groupId = challenge.GroupId;

        uint currentValue = 0;
        if (challenge.ChallengeType == "CHALLENGE_KILL_COUNT_FAST")
        {
            // For fast-kill challenges hk4e uses the kill-count condition
            // as the record source, so the settle/record value is the
            // number of defeated monsters in the chain.
            currentValue = challenge.KillCount;
        }
        else if (challenge.DurationSeconds > 0)
        {
            var now = DateTimeOffset.UtcNow;
            var elapsed = (uint)Math.Max(0, (now - challenge.StartTime).TotalSeconds);
            currentValue = challenge.DurationSeconds > elapsed ? challenge.DurationSeconds - elapsed : 0;
        }
        else if (challenge.Params.Count > 0)
        {
            uint p1 = challenge.Params[0];
            // Only propagate p1 as a value if it looks like a real time/count,
            // not a large group_id.
            if (p1 > 0 && p1 <= 86400)
            {
                currentValue = p1;
            }
        }

        var finish = new DungeonChallengeFinishNotify
        {
            ChallengeIndex = challenge.ChallengeIndex,
            IsSuccess = isSuccess,
            IsNewRecord = false,
            ChallengeRecordType = 0,
            CurrentValue = currentValue
        };

        session.SendPacket(finish);

        if (challenge.Group != null)
        {
            var evtType = isSuccess ? TriggerEventType.EVENT_CHALLENGE_SUCCESS : TriggerEventType.EVENT_CHALLENGE_FAIL;
            var args = new ScriptArgs((int)groupId, (int)evtType, (int)challenge.ChallengeIndex, (int)finish.CurrentValue);
            LuaManager.executeTriggersLua(session, challenge.Group, args);
        }

        if (_groupChallenges.TryGetValue(groupId, out var perGroup))
        {
            perGroup.Remove(challenge.ChallengeIndex);
            if (perGroup.Count == 0)
                _groupChallenges.Remove(groupId);
        }
    }

    private void UpdateChallenges()
    {
        if (_groupChallenges.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;

        foreach (var kv in _groupChallenges.ToArray())
        {
            var perGroup = kv.Value;
            foreach (var challenge in perGroup.Values.ToList())
            {
                // Special handling for CHALLENGE_KILL_COUNT_FAST: there is no
                // global challenge timer, only an interval between kills.
                if (challenge.ChallengeType == "CHALLENGE_KILL_COUNT_FAST")
                {
                    if (challenge.MaxIntervalBetweenKills == 0 || challenge.LastKillTime == default)
                        continue;

                    var intervalElapsed = (uint)Math.Max(0, (now - challenge.LastKillTime).TotalSeconds);
                    uint remainingInterval = intervalElapsed >= challenge.MaxIntervalBetweenKills
                        ? 0
                        : challenge.MaxIntervalBetweenKills - intervalElapsed;

                    if (remainingInterval != challenge.LastNotifiedRemaining)
                    {
                        challenge.LastNotifiedRemaining = remainingInterval;

                        // In hk4e the fast-kill timer is driven by a
                        // CHALLENGE_COND_KILL_FAST condition with
                        // param_index = 2. Use ParamIndex = 2 here so the HUD
                        // bar behaves the same.
                        var data = new ChallengeDataNotify
                        {
                            ChallengeIndex = challenge.ChallengeIndex,
                            ParamIndex = 2,
                            Value = remainingInterval
                        };
                        session.SendPacket(data);
                    }

                    if (remainingInterval == 0)
                    {
                        FinishChallengeInternal(challenge, false);
                    }

                    continue;
                }

                if (challenge.DurationSeconds == 0)
                    continue;

                var elapsed = (uint)Math.Max(0, (now - challenge.StartTime).TotalSeconds);
                uint remaining = challenge.DurationSeconds > elapsed ? challenge.DurationSeconds - elapsed : 0;

                if (remaining != challenge.LastNotifiedRemaining)
                {
                    challenge.LastNotifiedRemaining = remaining;

                    // Timer data is reported on the same param_index that the
                    // corresponding IN_TIME/ALL_TIME condition in hk4e uses.
                    uint timeParamIndex = 1;
                    switch (challenge.ChallengeType)
                    {
                        case "CHALLENGE_KILL_COUNT_IN_TIME":
                        case "CHALLENGE_TIME_FLY":
                        case "CHALLENGE_GUARD_HP":
                            timeParamIndex = 2;
                            break;
                        default:
                            timeParamIndex = 1;
                            break;
                    }

                    var data = new ChallengeDataNotify
                    {
                        ChallengeIndex = challenge.ChallengeIndex,
                        ParamIndex = timeParamIndex,
                        Value = remaining
                    };
                    session.SendPacket(data);

                    if (remaining == 0)
                    {
                        FinishChallengeInternal(challenge, false);
                    }
                }
            }
        }
    }
}
