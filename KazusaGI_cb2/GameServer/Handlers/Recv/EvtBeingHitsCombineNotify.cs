using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtBeingHitsCombineNotify
{
    [Packet.PacketCmdId(PacketId.EvtBeingHitsCombineNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        var logger = new Logger("EvtBeingHitsCombineNotify");
        var req = packet.GetDecodedBody<EvtBeingHitsCombineNotify>();

        var entityMgr = session.player.Scene.EntityManager;
        foreach (var hitInfo in req.EvtBeingHitInfoLists)
        {
            if (hitInfo?.AttackResult == null)
                continue;

            var attackResult = hitInfo.AttackResult;
            var sourceEntityId = attackResult.AttackerId;
            var targetEntityId = attackResult.DefenseId;

            entityMgr.TryGet(sourceEntityId, out var sourceEntity);
            entityMgr.TryGet(targetEntityId, out var targetEntity);

            if (sourceEntity == null)
            {
                if (!entityMgr.WasRecentlyRemoved(sourceEntityId))
                {
                    logger.LogError($"Entity not found (source={sourceEntityId})");
                    continue;
                }
            }
            if (targetEntity == null)
            {
                if (!entityMgr.WasRecentlyRemoved(targetEntityId))
                {
                    logger.LogError($"Entity not found (target={targetEntityId})");
                    continue;
                }
            }

            if (attackResult.Damage > 0 && targetEntity is IDamageable dmg)
            {
                dmg.ApplyDamage(attackResult.Damage, attackResult);
            }
        }

        session.SendPacket(req);
    }
}