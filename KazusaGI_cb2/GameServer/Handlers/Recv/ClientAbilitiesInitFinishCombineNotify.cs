using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleClientAbilitiesInitFinishCombineNotify
{
    [Packet.PacketCmdId(PacketId.ClientAbilitiesInitFinishCombineNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        ClientAbilitiesInitFinishCombineNotify req = packet.GetDecodedBody<ClientAbilitiesInitFinishCombineNotify>();
        foreach (var proto in req.EntityInvokeLists)
        {
            foreach (var invoke in proto.Invokes)
            {
                // Add to the InvokeNotifier list for proper forwarding
                //session.player!.ClientAbilityInitFinishNotifyList.AddEntry(invoke);

                // Handle the ability invoke on the entity if it exists
                if (session.entityMap.TryGetValue(invoke.EntityId, out GameServer.Entity? entity))
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
        }

        // Send the notifications
        //session.player!.ClientAbilityInitFinishNotifyList.Notify();
    }
}
