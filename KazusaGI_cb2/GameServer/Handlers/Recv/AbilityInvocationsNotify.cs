using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleAbilityInvocationsNotify
{
    [Packet.PacketCmdId(PacketId.AbilityInvocationsNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        AbilityInvocationsNotify req = packet.GetDecodedBody<AbilityInvocationsNotify>();

        foreach (var invoke in req.Invokes)
        {
            // Handle the ability invoke on the entity if it exists
            if (session.player.Scene.EntityManager.TryGet(invoke.EntityId, out GameServer.Entity? entity))
            {
                // For now, just log that we're handling the ability invoke
                // Later, when ability system is more complete, we can add proper handling
                //session.c.LogInfo($"Handling ability invoke for entity {invoke.EntityId}, argument type: {invoke.ArgumentType}");
                if (entity.abilityManager != null)
                {
                    _ = entity.abilityManager.HandleAbilityInvokeAsync(invoke);
                }
            }
            else
            {
                //session.c.LogWarning($"Failed to find entity {invoke.EntityId} for ability invoke");
                //session.SendPacket(new AbilityInvocationFailNotify()
                //{
                //	Reason = $"Failed to find entity {invoke.EntityId} for ability invoke",
                //	EntityId = invoke.EntityId,
                //	Invoke = invoke,
                //});
            }
        }

        // Forward the ability invocations to other peers according to ForwardType
        // This mirrors hk4e's behavior: the client specifies how the
        // invocation should be propagated, and the server forwards it
        // within the scene using the combat forwarder.
        if (req.Invokes.Count > 0)
        {
            CombatForwarder.Forward(session, req, req.ForwardType);
        }
    }
}
