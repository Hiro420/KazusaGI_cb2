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
        EvtCreateGadgetNotify req = packet.GetDecodedBody<EvtCreateGadgetNotify>();
        uint entityId = req.EntityId;
        uint gadgetId = req.ConfigId;
        Protocol.Vector pos = req.InitPos;
        ClientGadgetEntity gadgetEntity = new ClientGadgetEntity(session, gadgetId, req, Session.VectorProto2Vector3(pos), Vector3.Zero, entityId);

        if (req.OwnerEntityId != 0)
            gadgetEntity.OwnerEntityId = req.OwnerEntityId;

        session.player.Scene.EntityManager.Add(gadgetEntity);
        //session.c.LogError($"[WARNING] Entity ID collision when adding gadget {gadgetId} with entity ID {entityId}");
        // session.SendPacket(req);
    }
}