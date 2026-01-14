using KazusaGI_cb2.Protocol;
using System.Collections.Generic;
using System.Linq;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleClientAbilitiesInitFinishCombineNotify
{
    [Packet.PacketCmdId(PacketId.ClientAbilitiesInitFinishCombineNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        ClientAbilitiesInitFinishCombineNotify req = packet.GetDecodedBody<ClientAbilitiesInitFinishCombineNotify>();

        // Validate scene exists
        if (session.player.Scene == null)
        {
            session.c.LogError($"[ClientAbilitiesInitFinishCombineNotify] Scene is null for player {session.player.Uid}");
            return;
        }

        // Bounds check: max 50 entities per message (from hk4e)
        int entityCount = System.Math.Min(req.EntityInvokeLists.Count, 50);
        
        for (int entityIdx = 0; entityIdx < entityCount; entityIdx++)
        {
            var entityInvokeEntry = req.EntityInvokeLists[entityIdx];
            uint entityId = entityInvokeEntry.EntityId;

            // Find the entity
            if (!session.player.Scene.EntityManager.TryGet(entityId, out GameServer.Entity? entity))
            {
                session.c.LogWarning($"[ClientAbilitiesInitFinishCombineNotify] Failed to find entity {entityId}");
                continue;
            }

            // Validate entity has ability manager
            if (entity.abilityManager == null)
            {
                session.c.LogWarning($"[ClientAbilitiesInitFinishCombineNotify] Entity {entityId} has no ability manager");
                continue;
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
                    session.c.LogError($"[ClientAbilitiesInitFinishCombineNotify] Failed to initialize ability manager for entity {entityId}: {ex.Message}");
                    continue;
                }
            }

            // Process all ability invocations for this entity
            // Bounds check: max 50 invokes per entity (from hk4e)
            int invokeCount = System.Math.Min(entityInvokeEntry.Invokes.Count, 50);
            for (int i = 0; i < invokeCount; i++)
            {
                var invoke = entityInvokeEntry.Invokes[i];
                // Dispatch ability invoke to the entity's ability manager
                _ = entity.abilityManager.HandleAbilityInvokeAsync(invoke);
            }
        }

        session.SendPacket(req);
    }
}
