using KazusaGI_cb2.Protocol;
using System.Collections.Generic;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

/// <summary>
/// Handles ClientAbilityInitFinishNotify (CMD_ID: 1104)
/// Initializes abilities for a single entity when the client finishes loading them.
/// Exact implementation from hk4e server.
/// </summary>
internal class HandleClientAbilityInitFinishNotify
{
    [Packet.PacketCmdId(PacketId.ClientAbilityInitFinishNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        ClientAbilityInitFinishNotify req = packet.GetDecodedBody<ClientAbilityInitFinishNotify>();

        // Validate scene exists
        if (session.player.Scene == null)
        {
            session.c.LogError($"[ClientAbilityInitFinishNotify] Scene is null for player {session.player.Uid}");
            return;
        }

        // Find the entity by ID
        uint entityId = req.EntityId;
        if (!session.player.Scene.EntityManager.TryGet(entityId, out GameServer.Entity? entity))
        {
            session.c.LogWarning($"[ClientAbilityInitFinishNotify] Failed to find entity {entityId}");
            return;
        }

        // Validate entity has an ability manager
        if (entity.abilityManager == null)
        {
            session.c.LogWarning($"[ClientAbilityInitFinishNotify] Entity {entityId} has no ability manager");
            return;
        }

        // Initialize ability component if not already done
        if (!entity.abilityManager._isInitialized)
        {
            try
            {
                entity.abilityManager.Initialize();
            }
            catch (System.Exception ex)
            {
                session.c.LogError($"[ClientAbilityInitFinishNotify] Failed to initialize ability manager for entity {entityId}: {ex.Message}");
                return;
            }
        }

        // Process all ability invocations for this entity
        // Bounds check: max 50 invokes per message (from hk4e)
        int invokeCount = System.Math.Min(req.Invokes.Count, 50);
        for (int i = 0; i < invokeCount; i++)
        {
            var invoke = req.Invokes[i];
            // Dispatch ability invoke to the entity's ability manager
            _ = entity.abilityManager.HandleAbilityInvokeAsync(invoke);
        }

        session.SendPacket(req);
    }
}
