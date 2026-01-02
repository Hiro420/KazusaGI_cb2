using KazusaGI_cb2.GameServer.Lua;
using KazusaGI_cb2.Protocol;
using KazusaGI_cb2.Resource.Excel;
using SharpCompress.Common;
using System.Numerics;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleGadgetInteractReq
{
    // Mirrors hk4e GadgetHandler::onGadgetInteractReq basic flow:
    // - validate scene and entity
    // - distance check
    // - choose interact type
    // - optionally mutate gadget state (e.g. open chest)
    [Packet.PacketCmdId(PacketId.GadgetInteractReq)]
    public static void OnPacket(Session session, Packet packet)
    {
        var req = packet.GetDecodedBody<GadgetInteractReq>();

        var rsp = new GadgetInteractRsp
        {
            Retcode = (int)Retcode.RetSucc,
            GadgetEntityId = req.GadgetEntityId,
            GadgetId = req.GadgetId,
            OpType = req.OpType,
            InteractType = InteractType.InteractNone
        };

        var player = session.player;
        if (player == null || player.Scene == null)
        {
            rsp.Retcode = (int)Retcode.RetFail;
            session.SendPacket(rsp);
            return;
        }

        // Resolve entity
        if (!player.Scene.EntityManager.TryGet(req.GadgetEntityId, out var ientity) || ientity is not GadgetEntity entity)
        {
            // Match hk4e: entity not found -> RET_ENTITY_NOT_EXIST (504)
            rsp.Retcode = (int)Retcode.RetEntityNotExist;
            session.SendPacket(rsp);
            return;
        }

		// Distance check (hk4e uses a configurable radius; we approximate with a fixed one)
		const float interactRange = 6.0f; // reasonably close to hk4e behavior
        Vector3 playerPos = player.Pos;
        Vector3 gadgetPos = entity.Position;

        if (!Scene.IsInRange(gadgetPos, playerPos, interactRange))
        {
            // Match hk4e: too far -> RET_DISTANCE_LONG (520)
            rsp.Retcode = (int)Retcode.RetDistanceLong;
            session.SendPacket(rsp);
            return;
        }

        // Decide interact type based on gadget Excel type
        rsp.InteractType = GetInteractTypeForGadget(req.GadgetId);

        // Apply simple state changes for common interactions on FINISH
        if (req.OpType == InterOpType.InterOpFinish)
        {
            HandleFinishInteraction(session, entity, rsp.InteractType);
        }

        // hk4e returns the result code from the entity-specific handler; for now we
        // just forward the (possibly updated) Retcode in rsp.
        session.SendPacket(rsp);
    }

    private static InteractType GetInteractTypeForGadget(uint gadgetId)
    {
        if (!MainApp.resourceManager.GadgetExcel.TryGetValue(gadgetId, out var gadgetExcel))
            return InteractType.InteractNone;

        var type = gadgetExcel.type;

        return type switch
        {
            GadgetType_Excel.Chest => InteractType.InteractOpenChest,
            GadgetType_Excel.GatherPoint => InteractType.InteractGather,
            GadgetType_Excel.GatherObject => InteractType.InteractGather,
            GadgetType_Excel.RewardStatue => InteractType.InteractOpenStatue,
            _ => InteractType.InteractPickItem,
        };
    }

    private static void HandleFinishInteraction(Session session, GadgetEntity gadget, InteractType interactType)
    {
        // Approximate hk4e behavior for the most common cases.
        switch (interactType)
        {
            case InteractType.InteractOpenChest:
                {
					if (gadget.state == Resource.GadgetState.ChestLocked || gadget.state == Resource.GadgetState.Default)
						gadget.ChangeState(Resource.GadgetState.ChestOpened);

                    if (gadget._gadgetLua != null && !string.IsNullOrEmpty(gadget._gadgetLua.drop_tag))
                    {
                        DropManager.DropGadgetLoot(session, gadget);
                    }

					gadget.ForceKill();

					// Record one-off/persistent gadget consumption so it won't respawn
					// for this player in future sessions, mirroring hk4e gadget state.
					var lua = gadget._gadgetLua;
					if (session.player != null && lua != null && (lua.isOneoff || lua.persistent))
					{
						session.player.OpenedGadgets.Add((session.player.SceneId, lua.group_id, lua.config_id));
					}

					break;
                }

            case InteractType.InteractGather:

				// Gather points / gather objects: despawn the gadget and fire EVENT_GATHER
				// so Lua group scripts can react (e.g. KillEntityByConfigId, refresh, etc.).
				// same as InteractPickItem
				if (gadget is DropItemEntity entity && session.player?.Scene != null)
				{
					session.player.Scene.EntityManager.Remove(entity._EntityId, VisionType.VisionGatherEscape);
				}
				else if (gadget._gadgetLua != null && session.player?.Scene != null)
				{
					var scene = session.player.Scene;

					// Despawn with a specific gather vision reason.
					scene.EntityManager.Remove(gadget._EntityId, VisionType.VisionGatherEscape);

					// Notify Lua triggers listening for EVENT_GATHER.
					var group = scene.GetGroup((int)gadget._gadgetLua.group_id);
					if (group != null)
					{
						var args = new ScriptArgs((int)gadget._gadgetLua.group_id, (int)EventType.EVENT_GATHER, (int)gadget._gadgetLua.config_id)
						{
							source_eid = (int)gadget._EntityId
						};
						LuaManager.executeTriggersLua(session, group, args);
					}
				}
                else
                {
                    session.c.LogError("GadgetInteractReq: InteractGather on non-gadget entity");
                }
				break;

            case InteractType.InteractPickItem:
                // Drop item gadgets: at minimum, despawn on successful pickup.
                if (session.player?.Scene != null)
                {
                    session.player.Scene.EntityManager.Remove(gadget._EntityId, VisionType.VisionGatherEscape);
					// todo: fire EVENT_GATHER for Lua triggers and handle actual item granting
				}
				break;

            default:
                // Other interaction types are currently handled via gadget abilities / Lua.
                break;
        }
    }
}
