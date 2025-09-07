using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers;

public class Evt
{
    [Packet.PacketCmdId(PacketId.EvtBeingHitsCombineNotify)]
	public static void HandleEvtBeingHitsCombineNotify(Session session, Packet packet)
	{
		var logger = new Logger("EvtBeingHitsCombineNotify");
		var req = packet.GetDecodedBody<EvtBeingHitsCombineNotify>();

		foreach (var hitInfo in req.EvtBeingHitInfoLists)
		{
			if (hitInfo?.AttackResult == null)
				continue;

			var attackResult = hitInfo.AttackResult;
			var sourceEntityId = attackResult.AttackerId;
			var targetEntityId = attackResult.DefenseId;

			session.entityMap.TryGetValue(sourceEntityId, out var sourceEntity);
			session.entityMap.TryGetValue(targetEntityId, out var targetEntity);

			if (sourceEntity == null || targetEntity == null)
			{
				logger.LogError($"Entity not found (source={sourceEntityId}, target={targetEntityId})");
				continue;
			}

			if (attackResult.Damage > 0 && targetEntity is IDamageable dmg)
			{
				dmg.ApplyDamage(attackResult.Damage, attackResult);
			}
		}

		session.SendPacket(req);
	}


	[Packet.PacketCmdId(PacketId.AbilityInvocationsNotify)]
    public static void HandleAbilityInvocationsNotify(Session session, Packet packet)
    {
        AbilityInvocationsNotify req = packet.GetDecodedBody<AbilityInvocationsNotify>();
        // session.SendPacket(req);
    }

    [Packet.PacketCmdId(PacketId.EvtDoSkillSuccNotify)]
    public static void HandleEvtDoSkillSuccNotify(Session session, Packet packet)
    {
        EvtDoSkillSuccNotify req = packet.GetDecodedBody<EvtDoSkillSuccNotify>();
		// session.SendPacket(req);
	}

	[Packet.PacketCmdId(PacketId.EvtSetAttackTargetNotify)]
    public static void HandleEvtSetAttackTargetNotify(Session session, Packet packet)
    {
        // do nothing
    }

    [Packet.PacketCmdId(PacketId.ClientAbilitiesInitFinishCombineNotify)]
    public static void HandleClientAbilitiesInitFinishCombineNotify(Session session, Packet packet)
    {
        ClientAbilitiesInitFinishCombineNotify req = packet.GetDecodedBody<ClientAbilitiesInitFinishCombineNotify>();
		// session.SendPacket(req);
	}

	[Packet.PacketCmdId(PacketId.EvtFaceToDirNotify)]
    public static void HandleEvtFaceToDirNotify(Session session, Packet packet)
    {
        EvtFaceToDirNotify req = packet.GetDecodedBody<EvtFaceToDirNotify>();
		// session.SendPacket(req);
	}

	[Packet.PacketCmdId(PacketId.EvtAnimatorParameterNotify)]
    public static void HandleEvtAnimatorParameterNotify(Session session, Packet packet)
    {
        EvtAnimatorParameterNotify req = packet.GetDecodedBody<EvtAnimatorParameterNotify>();
		// session.SendPacket(req);
	}

	[Packet.PacketCmdId(PacketId.EvtEntityRenderersChangedNotify)]
    public static void HandleEvtEntityRenderersChangedNotify(Session session, Packet packet)
    {
        EvtEntityRenderersChangedNotify req = packet.GetDecodedBody<EvtEntityRenderersChangedNotify>();
		// session.SendPacket(req);
	}

	[Packet.PacketCmdId(PacketId.EvtAiSyncSkillCdNotify)]
    public static void HandleEvtAiSyncSkillCdNotify(Session session, Packet packet)
    {
        EvtAiSyncSkillCdNotify req = packet.GetDecodedBody<EvtAiSyncSkillCdNotify>();
		// session.SendPacket(req);
	}

	[Packet.PacketCmdId(PacketId.EvtCreateGadgetNotify)]
    public static void HandleEvtCreateGadgetNotify(Session session, Packet packet)
    {
        EvtCreateGadgetNotify req = packet.GetDecodedBody<EvtCreateGadgetNotify>();
        uint entityId = req.EntityId;
        uint gadgetId = req.ConfigId;
        Vector pos = req.InitPos;
		GadgetEntity gadgetEntity = new GadgetEntity(session, gadgetId, null, Session.VectorProto2Vector3(pos), System.Numerics.Vector3.Zero, entityId);
        if (!session.entityMap.TryAdd(entityId, gadgetEntity))
            session.c.LogError($"[WARNING] Entity ID collision when adding gadget {gadgetId} with entity ID {entityId}");
        // session.SendPacket(req);
    }

    [Packet.PacketCmdId(PacketId.EvtDestroyGadgetNotify)]
    public static void HandleEvtDestroyGadgetNotify(Session session, Packet packet)
    {
        EvtDestroyGadgetNotify req = packet.GetDecodedBody<EvtDestroyGadgetNotify>();
        uint entityId = req.EntityId;
        session.entityMap.Remove(entityId);
        // session.SendPacket(req);
    }

    [Packet.PacketCmdId(PacketId.MonsterAlertChangeNotify)]
    public static void HandleMonsterAlertChangeNotify(Session session, Packet packet)
    {
        // empty
    }
}