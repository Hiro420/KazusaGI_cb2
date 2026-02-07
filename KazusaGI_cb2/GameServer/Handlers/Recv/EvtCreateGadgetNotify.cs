using KazusaGI_cb2.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace KazusaGI_cb2.GameServer.Handlers.Recv;

internal class HandleEvtCreateGadgetNotify
{
    [Packet.PacketCmdId(PacketId.EvtCreateGadgetNotify)]
    public static void OnPacket(Session session, Packet packet)
    {
        EvtCreateGadgetNotify notify = packet.GetDecodedBody<EvtCreateGadgetNotify>();

        if (notify == null)
        {
            session.c.LogWarning("[EvtCreateGadgetNotify] get EvtCreateGadgetNotify failed");
            return;
        }

        var scene = session.player?.Scene;
        if (scene == null)
        {
            session.c.LogWarning("[EvtCreateGadgetNotify] scene_ptr is null");
            return;
        }

        uint entityId = notify.EntityId;

        if (entityId != 0 && scene.EntityManager.TryGet(entityId, out _))
        {
            session.c.LogWarning($"[EvtCreateGadgetNotify] entity_id: {entityId} is still alive!");
            return;
        }

        uint gadgetId = notify.ConfigId;
        Vector3 pos = Session.VectorProto2Vector3(notify.InitPos);
        Vector3 rot = Session.VectorProto2Vector3(notify.InitEulerAngles);

        Entity? ownerEntity = null;
        if (notify.OwnerEntityId != 0)
        {
            scene.EntityManager.TryGet(notify.OwnerEntityId, out ownerEntity);
        }

        if (entityId == 0)
        {
            entityId = scene.GenNewEntityId(ProtEntityType.ProtEntityGadget);
        }

        var gadgetEntity = new ClientGadgetEntity(session, gadgetId, notify, pos, rot, entityId);

        if (gadgetEntity == null)
        {
            session.c.LogWarning($"[EvtCreateGadgetNotify] createClientGadget fails, gadget_id: {gadgetId}");
            return;
        }

        if (ownerEntity != null)
        {
            gadgetEntity.OwnerEntityId = notify.OwnerEntityId;
        }

        scene.EntityManager.Add(gadgetEntity);

        // TODO: figure out why it breaks the client (do damage etc)
        //var appearNotify = new SceneEntityAppearNotify
        //{
        //    AppearType = VisionType.VisionMeet,
        //    EntityLists = { gadgetEntity.ToSceneEntityInfo() }
        //};
        //session.SendPacket(appearNotify);
    }
}